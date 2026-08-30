using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using HashCalculator.Others;

namespace HashCalculator;

/// <summary>
/// 哈希计算任务的调度器，负责任务的排队、并发调度、动态并发调整、待计算计数与状态发布。<br/>
/// 并发控制采用"活动并发数 + 目标并发数"的显式比较，调低并发时立即生效、
/// 正在运行的任务自然完成让位，避免并发数增减与调度循环派发之间的竞争。<br/>
/// 计数采用对称模型：Submit 时 +1，任务执行结束（无论完成或取消）时统一减 1，杜绝计数残留。<br/>
/// 界面上报由 DispatcherTimer 节流：定时器跟随工作状态启停（提交首个任务时启动、
/// 发布"已结束"时停止），Tick 中只在数值变化时通知 UI，任务提交与完成只做原子计数。<br/>
/// 需连续多次确认队列为空才发布"已结束"状态，避免计算快于文件搜索时"已结束"与"已开始"快速交替
/// 造成界面按钮状态抖动。
/// </summary>
public class JobScheduler : IDisposable
{
    /// <summary>
    /// 发布"已结束"所需的连续空队列确认次数（定时器间隔 × 本值）。<br/>
    /// 计算很快而文件搜索较慢时，队列可能被瞬间抽空又很快填入，
    /// 连续多次确认为空才发布 Stopped，避免界面按钮状态来回抖动。
    /// </summary>
    private const int CONFIRM_TICKS = 3;

    private readonly Channel<HashViewModel> channel;
    private readonly SemaphoreSlim concurrentSignal = new(0);
    private readonly CancellationTokenSource lifetime = new();

    /// <summary>
    /// 待计算任务数与工作状态的节流上报定时器。<br/>
    /// 跟随工作状态启停：提交首个任务（计数 0→1）时启动，确认队列为空并发布"已结束"后停止，
    /// 空闲时不再空转。<br/>
    /// 注意 DispatcherTimer 只能在创建它的线程上操作，启动必须投递到 UI 线程执行，从线程
    /// 池反复 Start/Stop 会使其内部状态错乱并假死。
    /// </summary>
    private readonly DispatcherTimer statusReportTimer;

    /// <summary>
    /// 目标并发数，即用户设置的"任务数"（允许同时计算的最大文件数）。<br/>
    /// 动态调整时只修改此值：调低后正在执行的任务自然完成让位，调度循环随即停止派发。
    /// </summary>
    private int targetTaskCount;

    /// <summary>
    /// 待计算任务数：已 Submit 但尚未执行结束的任务数量。<br/>
    /// 采用对称计数：Submit 时 +1，任务执行结束（无论完成或取消）时统一减 1，
    /// UI 显示的待计算行数即此值。
    /// </summary>
    private int pendingTaskCount;

    /// <summary>
    /// 当前正在执行的任务数，即已占用的并发数。<br/>
    /// 调度循环派发前将其与 targetTaskCount 比较，只有小于目标并发数时才派发新任务。
    /// </summary>
    private int executingTaskCount;

    /// <summary>
    /// 上一次上报给界面的待计算任务数，用于让常驻定时器只在值变化时上报。<br/>
    /// 该字段仅在定时器 Tick（界面线程）中读写，无需加锁或原子操作。
    /// </summary>
    private int reportedPendingCount;

    /// <summary>
    /// 连续检测到队列为空的 Tick 次数，达到 CONFIRM_TICKS 次数才发布"已结束"。<br/>
    /// 归零时机：队列非空时、发布"已结束"并停止定时器前、以及启动定时器时（StartReportTimerAction），
    /// 以保证每个工作周期的去抖都完整计数。
    /// </summary>
    private int pendingZeroTicks;

    private bool disposed = false;

    public JobScheduler(int initialConcurrency)
    {
        this.targetTaskCount = Math.Max(1, initialConcurrency);
        this.channel = Channel.CreateUnbounded<HashViewModel>(
            new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false,
            });
        this.statusReportTimer = new(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        this.statusReportTimer.Tick += this.StatusReportTimerTick;
        Task.Factory.StartNew(this.DispatchLoopAsync, this.lifetime.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// 待计算任务数发生变化（节流上报）
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
        this.lifetime.Cancel();
        this.lifetime.Dispose();
        this.statusReportTimer.Stop();
        this.channel.Writer.TryComplete();
        this.concurrentSignal.Dispose();
        GC.SuppressFinalize(this);
    }

    private void StatusReportTimerTick(object sender, EventArgs e)
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
            // 连续多次确认为空后发布"已结束"：计算快于文件搜索时队列会被
            // 抽空又很快填入，若立即发布"已结束"会与随后的"已开始"交替导致抖动
            if (++this.pendingZeroTicks >= CONFIRM_TICKS)
            {
                // 同时停止定时器，故不会重复发布"已结束"，下次提交首个任务时再启动
                this.pendingZeroTicks = 0;
                this.statusReportTimer.Stop();
                this.JobStatusChanged?.Invoke(JobStatus.Stopped);
            }
            return;
        }
        // 在 pending 不为零时重置确认计数，为下个工作周期的防抖执行初始化
        this.pendingZeroTicks = 0;
    }

    /// <summary>
    /// 把"启动上报定时器"投递到界面线程执行。<br/>
    /// Submit 运行在线程池线程，而 DispatcherTimer 只能在创建它的界面线程上操作，
    /// 直接跨线程 Start 会导致其状态错乱并假死，故必须投递。
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
        // 必须在此归零：Submit 后新任务可能在下次 Tick 前就已全部完成，
        // 此时下一 Tick 走的是空队列分支而非"队列非空"分支，后者才会归零。
        // 若不在此归零，上一轮去抖残留的计数会让本次去抖被缩短（例如残留 2 时
        // 只再计 1 次就满 3），导致"已结束"提前发布导致抖动。
        this.pendingZeroTicks = 0;
        if (!this.statusReportTimer.IsEnabled)
        {
            this.statusReportTimer.Start();
        }
    }

    /// <summary>
    /// 提交一个待计算任务（可在任意线程、任意批次调用）。
    /// </summary>
    public void Submit(HashViewModel model)
    {
        // 仅在待计算计数由 0 变为 1 时发布"已启动"并启动定时器，避免频繁发布状态
        if (Interlocked.Increment(ref this.pendingTaskCount) == 1)
        {
            this.JobStatusChanged?.Invoke(JobStatus.Started);
            this.StartTimerOnUiThread();
        }
        model.MarkAsWaiting();
        this.channel.Writer.TryWrite(model);
    }

    /// <summary>
    /// 动态调整目标并发数（任务数）。<br/>
    /// 调低：正在运行的任务自然完成让位，调度循环不再派发新任务直至活动并发数降到新目标；<br/>
    /// 调高：立即唤醒调度循环派发更多排队任务。
    /// </summary>
    public void SetConcurrency(int concurrency)
    {
        concurrency = Math.Max(1, concurrency);
        Volatile.Write(ref this.targetTaskCount, concurrency);
        // 调高时唤醒正在等待并发数有空余的调度循环
        // 调低时多释放的信号会被 while 重新检查消耗
        this.concurrentSignal.Release();
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
            Interlocked.Decrement(ref this.executingTaskCount);
            // 唤醒调度循环，使其重新检查并发数是否还有空余以派发下一个任务
            this.concurrentSignal.Release();
            // 无论计算正常完成还是被取消，都在此统一递减计数，保证增减对称
            if (Interlocked.Decrement(ref this.pendingTaskCount) < 0)
            {
                Interlocked.Exchange(ref this.pendingTaskCount, 0);
            }
        }
    }

    /// <summary>
    /// 调度循环：从队列取出任务，在目标并发数内启动执行。
    /// </summary>
    private async Task DispatchLoopAsync()
    {
        CancellationToken ct = this.lifetime.Token;
        await foreach (HashViewModel model in this.channel.Reader.ReadAllAsync(ct))
        {
            // 等待空余并发数（正在执行的任务数小于目标并发数）
            while (Volatile.Read(ref this.executingTaskCount) >= Volatile.Read(ref this.targetTaskCount))
            {
                await this.concurrentSignal.WaitAsync(ct);
            }
            if (ct.IsCancellationRequested)
            {
                break;
            }
            Interlocked.Increment(ref this.executingTaskCount);
            _ = this.RunModelAsync(model);
        }
    }
}
