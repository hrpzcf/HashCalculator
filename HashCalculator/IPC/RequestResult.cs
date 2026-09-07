namespace HashCalculator.IPC;

/// <summary>
/// <see cref="PipeCommandHost.RequestAsync"/> 的返回模型：
/// 承载请求结果（<see cref="RequestStatus"/>）以及服务端回传的可选数据段。
/// 数据段用 byte[] 承载，长度由实际响应决定，天然支持变长。
/// </summary>
internal sealed class RequestResult
{
    public RequestStatus Status { get; init; }

    /// <summary>服务端回传的数据段；仅当 Status 为 OK 且确实带数据时非空，否则为 null</summary>
    public byte[] Payload { get; init; }

    public static RequestResult OfStatus(RequestStatus status) => new()
    {
        Status = status
    };
}
