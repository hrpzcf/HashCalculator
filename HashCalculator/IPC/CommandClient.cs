using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HashCalculator.IPC;

internal static class CommandClient
{
    public const int DefaultTimeoutMs = 1000;

    /// <summary>
    /// 向指定实例发送一条命令并等待其处理结果。
    /// 连接失败返回 Unreachable / Timeout，调用方据此回退为本地处理。
    /// </summary>
    public static async Task<IPCMessageSendBack> SendAsync(
        string pipeName, IPCMessageKind kind, ReadOnlyMemory<byte> payload = default,
        int timeoutMs = DefaultTimeoutMs)
    {
        using NamedPipeClientStream client =
            new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        try
        {
            await client.ConnectAsync(timeoutMs);
        }
        catch (TimeoutException)
        {
            return IPCMessageSendBack.Of(IPCSendResult.Timeout);
        }
        catch (Exception)
        {
            // 目标已退出，或完整性级别不足（低 IL 访问管理员实例的高 IL 对象）
            return IPCMessageSendBack.Of(IPCSendResult.Unreachable);
        }
        try
        {
            IPCMessageHeader header = new()
            {
                Version = IPCMessageHeader.CurrentVersion,
                Kind = (uint)kind,
                PayloadBytes = (uint)payload.Length,
                SourcePid = (uint)Environment.ProcessId,
            };
            byte[] headerBytes = new byte[IPCMessageHeader.Size];
            MemoryMarshal.Write(headerBytes, in header);
            await client.WriteAsync(headerBytes);
            if (payload.Length != 0)
            {
                await client.WriteAsync(payload);
            }
            byte[] responseHeader = new byte[IPCResponseHeader.Size];
            await client.ReadExactlyAsync(responseHeader);
            IPCResponseHeader response = MemoryMarshal.Read<IPCResponseHeader>(responseHeader);
            // 响应头后的数据段是变长字节流，需按 PayloadBytes 再读一段，
            // 与请求方向（先读定长头再读变长 payload）对称。
            byte[] backPayload = new byte[response.PayloadBytes];
            if (backPayload.Length != 0)
            {
                await client.ReadExactlyAsync(backPayload);
            }
            IPCSendResult result = (IPCMessageStatus)response.Status switch
            {
                IPCMessageStatus.Ok => IPCSendResult.Delivered,
                IPCMessageStatus.Refused => IPCSendResult.Refused,
                _ => IPCSendResult.Failed,
            };
            if (result != IPCSendResult.Delivered || backPayload.Length == 0)
            {
                backPayload = default;
            }
            return new IPCMessageSendBack
            {
                Result = result,
                Payload = backPayload,
            };
        }
        catch (Exception)
        {
            return IPCMessageSendBack.Of(IPCSendResult.Failed);
        }
    }
}
