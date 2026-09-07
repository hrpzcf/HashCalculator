namespace HashCalculator.IPC;

/// <summary>
/// 跨进程命令类型，取值以 uint 传输。
/// 新增命令只能追加到末尾，不能插入或重排，否则与旧版 Shell 扩展通信时命令会被解析错位。
/// </summary>
internal enum HandlerIdentity : uint
{
    Unknown = 0,

    /// <summary>
    /// 执行一条待处理的命令行参数（compute/verify 等）
    /// Payload 为参数数组（ANSI，多个参数以 \0 分隔），首项为真实 verb，
    /// 不含可执行名占位符。接收方拆分后与命令行启动走同一套处理逻辑。
    /// </summary>
    ParseArguments = 1,

    Activate = 2,

    NavigateTo = 3,

    /// <summary>
    /// 查询本实例当前的多实例模式。
    /// 无 Payload；响应数据段为单字节 0/1（0 单实例，1 多实例）
    /// </summary>
    GetAppMultiMode = 4,

    /// <summary>
    /// 通知其他实例更新其多实例模式设置。
    /// Payload 为单字节 0/1（0 单实例，1 多实例），接收方据此更新自己的对应设置。
    /// </summary>
    SetAppMultiMode = 5,
}
