using System;
using System.Security.Principal;
using System.Threading;

namespace HashCalculator.IPC;

/// <summary>
/// 应用实例的跨进程占位与多实例模式发布。<br/>
/// 一条 per-user 命名事件同时承担两件事：<br/>
/// 1. 存在性证明：创建成功（createdNew）即本实例是首个实例；<br/>
/// 2. 模式发布：事件的有信号状态即 RunInMultiInstMode，后到者直接读取，<br/>
///    既不往返询问也不读配置文件。<br/>
/// 本地启动的实例都持有句柄不释放，转发后即退出的实例不提句柄，
/// 因此内核对象的存在期就等于应用的存在期。<br/>
/// 用事件而不是 Mutex：它的状态就是我们那一位，WaitOne(0) 是纯读不阻塞，
/// Set()/Reset() 是一次原子写，不存在"先建新的再放旧的"这种中间态。
/// </summary>
internal static class InstanceClaim
{
    // 初始无信号 = 单实例模式：首个实例在加载设置、发布真实值之前的这段时间里，
    // 后到者读到的就是这个初始值；取"单实例"是因为它让判断倒向保守侧——
    // 单实例模式（本功能要修的场景）下后到者一定让位于首个实例，不会并存两个实例。
    private static readonly string ClaimEventName = @"Local\HashCalculator." +
        WindowsIdentity.GetCurrent().User.Value;

    private static EventWaitHandle claimEvent;

    /// <summary>
    /// true = 本实例是首个实例（应用此前不在运行）
    /// </summary>
    public static bool TryClaim()
    {
        try
        {
            claimEvent = new EventWaitHandle(false, EventResetMode.ManualReset,
                ClaimEventName, out bool createdNew);
            return createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            // 同用户但提升级别不同：对方的内核对象打不开，它的管道也连不上，
            // 此时退出等于"点了没反应"，故按本地启动处理。
            claimEvent = null;
            return false;
        }
    }

    /// <summary>
    /// 把本实例的多实例模式发布出去，供之后启动的实例读取
    /// </summary>
    public static void PublishMultiInstMode(bool runInMultiInstMode)
    {
        if (claimEvent is null)
        {
            return;
        }
        if (runInMultiInstMode)
        {
            claimEvent.Set();
        }
        else
        {
            claimEvent.Reset();
        }
    }

    /// <summary>
    /// 读现存实例发布的模式位；返回 false 表示读不到（打不开内核对象）
    /// </summary>
    public static bool TryReadMultiInstMode(out bool runInMultiInstMode)
    {
        if (claimEvent is null)
        {
            runInMultiInstMode = false;
            return false;
        }
        runInMultiInstMode = claimEvent.WaitOne(0);
        return true;
    }
}
