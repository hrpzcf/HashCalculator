namespace HashCalculator.IPC;

internal enum HandlerStatus : uint
{
    /// <summary>
    /// Handler 已经成功执行命令
    /// </summary>
    OK = 0,

    /// <summary>
    /// Handler 执行命令时发生异常
    /// </summary>
    Error = 1,

    /// <summary>
    /// 请求负载的长度或格式不符合该命令要求
    /// </summary>
    BadPayload = 2,

    /// <summary>
    /// 传入的命令不在 Handler 的已知命令内
    /// </summary>
    BadCommand = 3,
}
