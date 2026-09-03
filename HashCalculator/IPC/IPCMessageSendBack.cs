namespace HashCalculator.IPC;

/// <summary>
/// <see cref="CommandClient.SendAsync"/> 的返回模型：
/// 承载发送结果（<see cref="IPCSendResult"/>）以及服务端回传的可选数据段。
/// 数据段用 byte[] 承载，长度由实际响应决定，天然支持变长。
/// </summary>
internal sealed class IPCMessageSendBack
{
    public IPCSendResult Result { get; init; }

    /// <summary>服务端回传的数据段；仅当 Result 为 Delivered 且确实带数据时非空，否则为 null</summary>
    public byte[] Payload { get; init; }

    public static IPCMessageSendBack Of(IPCSendResult result) => new()
    {
        Result = result
    };
}
