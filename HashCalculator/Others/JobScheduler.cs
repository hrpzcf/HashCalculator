using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using HashCalculator.Others;

namespace HashCalculator;

/// <summary>
/// 哈希计算任务的调度器：负责任务排队、并发调度、动态并发调整、启动/暂停/继续/取消、
/// 待计算计数与状态发布。<br/>
/// 作业分 queuedJobs（已提交未派发）与 runningJobs（已派发，含被暂停的）两个集合，
/// 派发时从前者移入后者、执行结束移出；控制命令一律以这两个集合定向执行，无需遍历全表，
/// 也不存在派发期间的漏网窗口。<br/>
/// 并发控制为"运行中作业数 vs 目标并发数"的显式比较：派发闸关闭时调度循环停止取任务，
/// 使"全部暂停"一次冻结整个队列。<br/>
/// 计数对称：提交时 +1，执行结束或排队中被取消时 -1。<br/>
/// 界面上报由 DispatcherTimer 节流并跟随工作状态启停；连续多次确认队列为空才发布"已结束"，
/// 避免计算快于文件搜索时状态抖动。<br/>
/// 注意：控制方法读写作业的界面绑定属性，必须且只需在界面线程调用。
/// </summary>
public class JobScheduler : IDisposable
{
    /// <summary>
    /// 发布"已结束"所需的连续空队列确认次数（定时器间隔 × 本值），
    /// 避免计算快于文件搜索时"已结束/已开始"交替造成的按钮状态抖动。
    /// </summary>
    private const int CONFIRM_TICKS = 3;

    /// <summary>
    /// 待计算作业数与工作状态的节流上报定时器，跟随工作状态启停：提交首个作业时启动、
    /// 发布"已结束"后停止，空闲时不空转。<br/>
    /// DispatcherTimer 只能在创建它的线程上操作，启动必须投递到界面线程，从线程池反复 
    /// Start/Stop 会使其状态错乱并假死。
    /// </summary>
    private readonly DispatcherTimer statusReportTimer;

    /// <summary>
    /// 两个作业集合的同步根。派发的原子性依赖此锁：判断条件、出队、设置暂停信号、
    /// 入 runningJobs 必须一气呵成，否则控制命令的快照会与派发交错而漏掉作业。<br/>
    /// 锁内只允许集合操作与"仅动暂停信号、不碰界面属性"的调用，任何会回界面线程或调用
    /// 作业自身逻辑的操作都必须放到锁外，以免与持有本锁的界面线程互等。
    /// </summary>
    private readonly object schedulerStateLock = new object();

    /// <summary>
    /// 已派发的作业（含被暂停的）。被暂停的作业不退出因而持续占用并发数，这是"暂停 N 个
    /// 只剩 N 路继续派发"的预期行为，也使"全部暂停"能真正让队列停住；元素个数即占用的并发数。
    /// </summary>
    private readonly HashSet<HashViewModel> runningJobs = new();

    /// <summary>
    /// 已提交但尚未派发的作业，按提交顺序排列。
    /// </summary>
    private readonly LinkedList<HashViewModel> queuedJobs = new();

    private bool disposed = false;

    /// <summary>
    /// 调度循环退出标志，Dispose 时置位并唤醒阻塞在 Monitor.Wait 上的循环。
    /// </summary>
    private volatile bool stopped = false;

    /// <summary>
    /// 派发闸：置位后调度循环停止取作业，使"全部暂停"一次冻结整个队列。
    /// 在 schedulerStateLock 内读写，声明 volatile 以便锁外只读也能看到最新值。
    /// </summary>
    private volatile bool dispatchPaused = false;

    /// <summary>
    /// 目标并发数，即用户设置的"任务数"（允许同时计算的最大文件数）。动态调整只改此值：
    /// 调低后正在执行的作业自然完成让位，调度循环随即停止派发。
    /// </summary>
    private int targetTaskCount;

    /// <summary>
    /// 待计算作业数：已提交但尚未执行结束的作业数量。采用对称计数
    /// （入队 +1，执行结束或排队中被取消时 -1），界面显示的待计算行数即此值。
    /// </summary>
    private int pendingTaskCount;

    /// <summary>
    /// 上一次上报给界面的待计算作业数，使常驻定时器只在值变化时上报。
    /// 仅在定时器 Tick（界面线程）中读写，无需加锁。
    /// </summary>
    private int reportedPendingCount;

    /// <summary>
    /// 连续检测到队列为空的 Tick 次数，达到 CONFIRM_TICKS 才发布"已结束"。
    /// 在队列非空、发布"已结束"、启动定时器（StartReportTimerAction）时归零，
    /// 保证每轮去抖都完整计数。
    /// </summary>
    private int pendingZeroTicks;

    /// <summary>
    /// 当前唯一的调度器实例。行内任务控制按钮的绑定目标是 HashViewModel，
    /// 无法经数据上下文拿到调度器，故提供此静态入口转交控制请求。
    /// </summary>
    public static JobScheduler Current { get; private set; }

    public JobScheduler(int initialConcurrency)
    {
        Current = this;
        this.targetTaskCount = Math.Max(1, initialConcurrency);
        this.statusReportTimer = new(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        this.statusReportTimer.Tick += this.StatusReportTick;
        // 调度循环全程阻塞在 Monitor.Wait，必须用专用线程：
        // 占用线程池线程会挤占 Task.Run 的计算任务，导致并发数达不到设定值
        Task.Factory.StartNew(this.DispatchLoop, CancellationToken.None,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// 待计算作业数发生变化（节流上报）
    /// </summary>
    public event Action<int> PendingCountChanged;

    /// <summary>
    /// 整体运行状态发生变化（Started / Stopped）
    /// </summary>
    public event Action<JobStatus> JobStatusChanged;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }
        this.disposed = true;
        lock (this.schedulerStateLock)
        {
            this.stopped = true;
            Monitor.PulseAll(this.schedulerStateLock);
        }
        this.statusReportTimer.Stop();
        GC.SuppressFinalize(this);
    }

    private void StatusReportTick(object sender, EventArgs e)
    {
        int pending = Volatile.Read(ref this.pendingTaskCount);
        // 值未变化时不重复上报，空闲时不再无谓刷新 UI 界面
        if (pending != this.reportedPendingCount)
        {
            this.reportedPendingCount = pending;
            this.PendingCountChanged?.Invoke(pending);
        }
        if (pending == 0)
        {
            // 连续多次确认为空后发布"已结束"，避免计算快于文件搜索时状态抖动
            if (++this.pendingZeroTicks >= CONFIRM_TICKS)
            {
                // 同时停止定时器，故不会重复发布"已结束"，下次提交首个任务时再启动
                this.pendingZeroTicks = 0;
                this.statusReportTimer.Stop();
                this.JobStatusChanged?.Invoke(JobStatus.Stopped);
            }
            return;
        }
        // 当 pending 不为零时重置确认计数，为下个周期的去抖初始化
        this.pendingZeroTicks = 0;
    }

    /// <summary>
    /// 把"启动上报定时器"投递到界面线程执行：提交可能发生在非界面线程，而 DispatcherTimer
    /// 只能在创建它的界面线程上操作，直接跨线程 Start 会导致其状态错乱并假死。
    /// </summary>
    private void StartTimerOnUiThread()
    {
        Dispatcher uiDispatcher = Synchronization.UI;
        if (!uiDispatcher.CheckAccess())
        {
            uiDispatcher.BeginInvoke(this.StartReportTimerAction);
        }
        else
        {
            this.StartReportTimerAction();
        }
    }

    /// <summary>
    /// 在 UI 界面线程确保定时器处于启动状态，并重置空队列确认计数。
    /// </summary>
    private void StartReportTimerAction()
    {
        // 必须在此归零：新任务可能在下次 Tick 前就已全部完成，此时下一 Tick
        // 走空队列分支而非会归零的"队列非空"分支；不归零则上轮残留会让本次去抖被缩短，
        // 导致"已结束"提前发布而抖动。
        this.pendingZeroTicks = 0;
        if (!this.statusReportTimer.IsEnabled)
        {
            this.statusReportTimer.Start();
        }
    }

    /// <summary>
    /// 把作业放入排队队列，并递增待计算计数。必须在持有 schedulerStateLock 时调用。
    /// </summary>
    private void EnqueueHashViewModel(HashViewModel model)
    {
        if (Interlocked.Increment(ref this.pendingTaskCount) == 1)
        {
            // 计数由 0 变 1，首个任务：发布"已开始"并启动定时器
            this.JobStatusChanged?.Invoke(JobStatus.Started);
            this.StartTimerOnUiThread();
        }
        model.SetStateAsync(HashState.Waiting);
        this.queuedJobs.AddLast(model);
        Monitor.PulseAll(this.schedulerStateLock);
    }

    /// <summary>
    /// 动态调整目标并发数。调低时正在运行的作业自然完成让位，调度循环不再派发新任务
    /// 直至运行中作业数降到新目标；调高时立即唤醒调度循环派发更多排队作业。
    /// </summary>
    public void SetConcurrency(int concurrency)
    {
        Volatile.Write(ref this.targetTaskCount, Math.Max(1, concurrency));
        lock (this.schedulerStateLock)
        {
            // 调高时唤醒因并发数已满而等待的调度循环；调低时多释放的唤醒会被 while 重新检查消耗
            Monitor.PulseAll(this.schedulerStateLock);
        }
    }

    /// <summary>
    /// 启动一个任务：判断启动条件后入队。
    /// </summary>
    public int Start(HashViewModel job, ComputeIntent intent)
    {
        return this.Start(new[] { job }, intent);
    }

    /// <summary>
    /// 启动一批任务：逐个询问作业能否按该意图启动，锁内完成收集；重建作业状态在锁外执行，
    /// 避免持锁回界面线程。准入规则由作业自身（HashViewModel.CanStart）提供。
    /// </summary>
    public int Start(IEnumerable<HashViewModel> jobs, ComputeIntent intent)
    {
        // 复制成数组固定本次操作范围，jobs 本身是数组时直接复用避免重复分配
        HashViewModel[] targets = jobs as HashViewModel[] ?? jobs.ToArray();
        List<HashViewModel> accepted = new List<HashViewModel>(targets.Length);
        lock (this.schedulerStateLock)
        {
            foreach (HashViewModel job in targets)
            {
                if (job.CanStart(intent))
                {
                    accepted.Add(job);
                }
            }
        }
        // 重置作业状态触发界面通知，必须在锁外
        foreach (HashViewModel job in accepted)
        {
            job.PrepareForRestartModel(intent);
        }
        if (accepted.Count == 0)
        {
            return 0;
        }
        lock (this.schedulerStateLock)
        {
            foreach (HashViewModel job in accepted)
            {
                this.EnqueueHashViewModel(job);
            }
        }
        return accepted.Count;
    }

    /// <summary>
    /// 把作业标记为暂停态。暂停信号已在锁内设置完毕，此处只补界面状态：
    /// 即便界面状态更新晚于作业的后续变更，也不会把已暂停的作业重新显示为运行中。
    /// </summary>
    private void MarkModelsAsPaused(IEnumerable<HashViewModel> jobs)
    {
        foreach (HashViewModel job in jobs)
        {
            job.SetStateAsync(HashState.Paused);
        }
    }

    /// <summary>
    /// 暂停指定作业。仅已派发的作业可被暂停，排队的作业尚未开始计算，只能取消。
    /// </summary>
    public void Pause(HashViewModel job)
    {
        this.Pause(new[] { job });
    }

    /// <summary>
    /// 暂停指定作业。仅已派发的作业可被暂停，排队的作业尚未开始计算，只能取消。
    /// 不关闭派发闸：被暂停的作业持续占用并发数，派发速度随之下降，这是预期语义。
    /// </summary>
    public void Pause(IEnumerable<HashViewModel> jobs)
    {
        // 取目标集合的快照：调用方传入的可能是在不断变化的可变集合，
        // 复制成数组固定本次操作范围，jobs 本身是数组时直接复用避免重复分配
        HashViewModel[] targets = jobs as HashViewModel[] ?? jobs.ToArray();
        // 供锁外的 MarkModelsAsPaused 使用：
        // runningJobs 非线程安全，用 running 锁内取"目标中已派发作业"的快照
        // 改界面状态会回界面线程，不能持锁，故必须在锁内筛好
        HashSet<HashViewModel> running = new HashSet<HashViewModel>();
        lock (this.schedulerStateLock)
        {
            foreach (HashViewModel job in targets)
            {
                if (this.runningJobs.Contains(job))
                {
                    running.Add(job);
                    job.SetPauseSignal(true);
                }
            }
        }
        this.MarkModelsAsPaused(running);
    }

    /// <summary>
    /// 全部暂停：关闭派发闸并暂停所有已派发的作业。只关闸不够（在飞作业会一路算完），
    /// 只暂停在飞作业也不够（排队的仍会被派发），两者缺一不可。
    /// </summary>
    public void PauseAll()
    {
        HashViewModel[] running = null;
        lock (this.schedulerStateLock)
        {
            this.dispatchPaused = true;
            running = this.runningJobs.ToArray();
            foreach (HashViewModel job in running)
            {
                job.SetPauseSignal(true);
            }
        }
        this.MarkModelsAsPaused(running);
    }

    /// <summary>
    /// 反转单个作业的暂停状态。
    /// </summary>
    public void PauseOrResume(HashViewModel job)
    {
        if (job is null)
        {
            return;
        }
        bool dispatched = false;
        lock (this.schedulerStateLock)
        {
            dispatched = this.runningJobs.Contains(job);
        }
        if (!dispatched)
        {
            // 从未启动过的作业，"继续"等同于启动
            if (job.State == HashState.NoState)
            {
                job.PrepareForRestartModel(ComputeIntent.FillMissing);
                lock (this.schedulerStateLock)
                {
                    this.EnqueueHashViewModel(job);
                }
            }
            return;
        }
        // 暂停与否由实际暂停信号裁定：信号同步且权威，不受 State 异步窗口期影响
        bool paused = job.IsPaused;
        job.SetPauseSignal(!paused);
        job.SetStateAsync(paused ? HashState.Running : HashState.Paused);
    }

    /// <summary>
    /// 继续指定作业：唤醒已派发且被暂停的作业；从未启动过的作业则直接启动。
    /// </summary>
    public void Resume(HashViewModel job)
    {
        this.Resume(new[] { job });
    }

    /// <summary>
    /// 继续指定作业：唤醒已派发且被暂停的作业；从未启动过的作业则直接启动。
    /// </summary>
    public void Resume(IEnumerable<HashViewModel> jobs)
    {
        // 取目标集合的快照：调用方传入的可能是在不断变化的可变集合，
        // 复制成数组固定本次操作范围，jobs 本身是数组时直接复用避免重复分配
        HashViewModel[] targets = jobs as HashViewModel[] ?? jobs.ToArray();
        // 用 running 锁内取"目标中已派发作业"的快照，供锁外使用：
        // runningJobs 非线程安全，且锁外的分支处理会回界面线程（SetStateAsync），
        // 不能持锁，故必须先在锁内把结果筛好
        HashSet<HashViewModel> running = new HashSet<HashViewModel>();
        lock (this.schedulerStateLock)
        {
            foreach (HashViewModel job in targets)
            {
                if (this.runningJobs.Contains(job))
                {
                    running.Add(job);
                    job.SetPauseSignal(false);
                }
            }
        }
        foreach (HashViewModel job in targets)
        {
            if (running.Contains(job))
            {
                job.SetStateAsync(HashState.Running);
            }
            else if (job.State == HashState.NoState)
            {
                job.PrepareForRestartModel(ComputeIntent.FillMissing);
                lock (this.schedulerStateLock)
                {
                    this.EnqueueHashViewModel(job);
                }
            }
        }
    }

    /// <summary>
    /// 全部继续：打开派发闸并唤醒所有被暂停的作业。
    /// </summary>
    public void ResumeAll()
    {
        HashViewModel[] running = null;
        lock (this.schedulerStateLock)
        {
            this.dispatchPaused = false;
            running = this.runningJobs.ToArray();
            foreach (HashViewModel job in running)
            {
                job.SetPauseSignal(false);
            }
            Monitor.PulseAll(this.schedulerStateLock);
        }
        foreach (HashViewModel job in running)
        {
            job.SetStateAsync(HashState.Running);
        }
    }

    /// <summary>
    /// 取消指定作业。
    /// </summary>
    public void Cancel(HashViewModel job)
    {
        this.Cancel(new[] { job });
    }

    /// <summary>
    /// 取消指定作业：排队的作业直接出队并递减计数，在飞的作业发取消信号后尽快退出。
    /// </summary>
    public void Cancel(IEnumerable<HashViewModel> jobs)
    {
        // 取目标集合的快照：调用方传入的可能是在不断变化的可变集合，
        // 复制成数组固定本次操作范围，jobs 本身是数组时直接复用避免重复分配
        HashViewModel[] targets = jobs as HashViewModel[] ?? jobs.ToArray();
        lock (this.schedulerStateLock)
        {
            foreach (HashViewModel job in targets)
            {
                // 出队成功说明该作业尚未派发，不会走到 RunModelAsync，
                // 其计数必须在此递减，与执行结束时的递减二者互斥
                if (this.queuedJobs.Remove(job))
                {
                    Interlocked.Decrement(ref this.pendingTaskCount);
                }
            }
        }
        foreach (HashViewModel job in targets)
        {
            job.RequestCancellation();
            // 尚未派发的作业不会走到执行结束流程，其状态需在此终结
            job.MarkCanceled();
        }
    }

    /// <summary>
    /// 全部取消：排队的作业直接出队并递减计数，在飞的作业发取消信号后由各自的执行结束流程收尾。
    /// </summary>
    public void CancelAll()
    {
        HashViewModel[] all = null;
        lock (this.schedulerStateLock)
        {
            all = this.queuedJobs.Concat(this.runningJobs).ToArray();
            // 排队的作业不会走到 RunModelAsync，其计数必须在此递减；
            // 在飞的作业由各自的 finally 递减，两处互斥，保证增减对称
            foreach (HashViewModel job in this.queuedJobs)
            {
                Interlocked.Decrement(ref this.pendingTaskCount);
            }
            this.queuedJobs.Clear();
            // 复位派发闸：用户此前点过"暂停"时 dispatchPaused 会一直为 true，
            // 不复位则后续新添加的任务全入队却不被派发。取消表示"全部停止并恢复待命"。
            this.dispatchPaused = false;
            Monitor.PulseAll(this.schedulerStateLock);
        }
        foreach (HashViewModel job in all)
        {
            job.RequestCancellation();
            // 尚未派发的作业不会走到执行结束流程，其状态需在此终结
            job.MarkCanceled();
        }
    }

    private async Task RunModelAsync(HashViewModel model)
    {
        try
        {
            // 是同步阻塞方法，在专用线程上运行以避免阻塞线程池
            await Task.Run(model.ComputeManyHashValue);
        }
        finally
        {
            lock (this.schedulerStateLock)
            {
                // 必须先移出运行集合再改界面状态：顺序颠倒会出现"状态已是 Finished
                // 却仍在运行集合里"，此时全部暂停会把它捞出来改成 Paused
                this.runningJobs.Remove(model);
                Monitor.PulseAll(this.schedulerStateLock);
            }
            model.SetStateAsync(HashState.Finished);
            // 无论计算正常完成还是被取消，都在此统一递减计数，保证增减对称
            if (Interlocked.Decrement(ref this.pendingTaskCount) < 0)
            {
                Interlocked.Exchange(ref this.pendingTaskCount, 0);
            }
        }
    }

    /// <summary>
    /// 调度循环：在派发闸开启且并发数有空余时，从队列取出作业并启动执行。
    /// 判断派发条件、出队、设置暂停信号、入运行集合在同一锁内一气呵成，故
    /// 控制命令的快照不会与派发交错。<br/>
    /// 循环全程阻塞在 Monitor.Wait，运行在专用线程。
    /// </summary>
    private void DispatchLoop()
    {
        while (true)
        {
            HashViewModel model;
            lock (this.schedulerStateLock)
            {
                while (!this.stopped &&
                       (this.dispatchPaused
                        || this.runningJobs.Count >= this.targetTaskCount
                        || this.queuedJobs.Count == 0))
                {
                    Monitor.Wait(this.schedulerStateLock);
                }
                if (this.stopped)
                {
                    return;
                }
                model = this.queuedJobs.First.Value;
                this.queuedJobs.RemoveFirst();
                // 暂停信号必须在入运行集合前解除：若放到锁外，作业可能先被全部暂停捞去
                // 置了暂停信号，随后又被此处的解除覆盖，导致漏网
                model.SetPauseSignal(false);
                this.runningJobs.Add(model);
            }
            model.SetStateAsync(HashState.Running);
            _ = this.RunModelAsync(model);
        }
    }
}
