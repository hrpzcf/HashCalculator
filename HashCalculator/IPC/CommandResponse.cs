namespace HashCalculator.IPC;

/// <summary>
/// Handler 对命令的处理结果，携带状态与可选的响应数据段。<br/>
/// 这是 C# 侧的内存模型，不走管道字节布局；
/// 传输时由服务端把 Status 与 Payload 序列化到 <see cref="IPCResponseHeader"/>
/// 之后紧跟的数据段（分两次写），客户端读回后还原成此模型。
/// </summary>
internal sealed class CommandResponse
{
    public static readonly CommandResponse Ok = new()
    {
        Status = IPCMessageStatus.Ok
    };

    public IPCMessageStatus Status { get; init; }

    /// <summary>
    /// 可选响应数据段；查询类命令（如 GetAppMultiMode）用它回传值，无数据时为 null
    /// </summary>
    public byte[] Payload { get; init; }

    public static CommandResponse FromPayload(byte[] payload) => new()
    {
        Status = IPCMessageStatus.Ok,
        Payload = payload,
    };
}
