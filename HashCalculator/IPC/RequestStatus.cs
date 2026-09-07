namespace HashCalculator.IPC;

internal enum RequestStatus
{
    /// <summary>
    /// 送达且被服务端接受
    /// </summary>
    OK = 0,

    /// <summary>
    /// 目标不存在或权限不足（完整性级别拦截）
    /// </summary>
    Unreachable = 1,

    Failed = 2,

    Timeout = 3,
}
