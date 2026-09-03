namespace HashCalculator.IPC;

/// <summary>
/// 一个存活实例的信息，由管道名解析得到。
/// 管道名格式为 HashCalculator.Ipc.{PID}.{启动时间戳}，
/// 因此无需连接即可得知各实例的进程 ID 与启动先后。
/// </summary>
internal sealed class InstanceEndpoint(string name, int id, long ticks)
{
    /// <summary>
    /// 不含有 \\.\pipe\ 前缀的管道名
    /// </summary>
    public string PipeName { get; } = name;

    public int ProcessId { get; } = id;

    /// <summary>
    /// 进程启动时的 UTC Ticks，用于精确判断启动先后
    /// </summary>
    public long StartTicks { get; } = ticks;
}
