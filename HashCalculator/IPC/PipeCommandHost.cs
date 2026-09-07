using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.IPC.Handlers;
using HashCalculator.Others;

namespace HashCalculator.IPC;

/// <summary>
/// 跨进程管道命令的宿主，每个 HashCaclulator 实例监听一条以自己进程 ID 命名的管道。<br/>
/// 常驻 ListenerInstanceCount 个并行的监听循环：Shell 扩展常会连续快速发起多条请求，
/// 若只有一个监听循环，则在"accept 一条、补建下一条 namedPipeServer"的空窗里接不住紧随其后的连接，
/// 故常驻多个监听循环互相兜底。每个监听循环处理完请求后在 PipeHandleAsync 里 DisposeAsync。
/// </summary>
internal static class PipeCommandHost
{
    private const int DefaultTimeoutMs = 200;
    private const int ListenerInstanceCount = 2;
    private const int InOutBufferSize = 64 * 1024;

    private static readonly CancellationTokenSource cts = new();
    private static readonly Dictionary<HandlerIdentity, IHandler> handlers = new();

    private static bool _listeningLoopRunning = false;

    /// <summary>
    /// 向指定实例发送一条命令并等待其处理结果。
    /// 连接失败返回 Unreachable / Timeout，调用方据此回退为本地处理。
    /// </summary>
    public static async Task<RequestResult> RequestAsync(
        string pipeName, HandlerIdentity identity, ReadOnlyMemory<byte> payload = default,
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
            return RequestResult.OfStatus(RequestStatus.Timeout);
        }
        catch (Exception)
        {
            // 目标已退出，或完整性级别不足（低 IL 访问管理员实例的高 IL 对象）
            return RequestResult.OfStatus(RequestStatus.Unreachable);
        }
        try
        {
            RequestHeader header = new RequestHeader()
            {
                Version = RequestHeader.CurrentVer,
                Identity = identity,
                PayloadBytes = (uint)payload.Length,
            };
            byte[] headerBytes = new byte[RequestHeader.Size];
            MemoryMarshal.Write(headerBytes, in header);
            await client.WriteAsync(headerBytes);
            if (payload.Length != 0)
            {
                await client.WriteAsync(payload);
            }
            byte[] responseHeader = new byte[ResponseHeader.Size];
            await client.ReadExactlyAsync(responseHeader);
            ResponseHeader response = MemoryMarshal.Read<ResponseHeader>(responseHeader);
            // 响应头后的数据段是变长字节流，需按 PayloadBytes 再读一段，
            // 与请求方向（先读定长头再读变长 payload）对称。
            byte[] backPayload = new byte[response.PayloadBytes];
            if (backPayload.Length != 0)
            {
                await client.ReadExactlyAsync(backPayload);
            }
            RequestStatus status = response.Status switch
            {
                HandlerStatus.OK => RequestStatus.OK,
                _ => RequestStatus.Failed,
            };
            if (status != RequestStatus.OK || backPayload.Length == 0)
            {
                backPayload = default;
            }
            return new RequestResult
            {
                Status = status,
                Payload = backPayload,
            };
        }
        catch (Exception)
        {
            return RequestResult.OfStatus(RequestStatus.Failed);
        }
    }

    /// <summary>
    /// 启动本实例的管道监听，只应执行一次
    /// </summary>
    public static void Start()
    {
        if (_listeningLoopRunning)
        {
            return;
        }
        _listeningLoopRunning = true;
        RegisterHandler(
            new ActivateAppHandler(),
            new GetMultiModeHandler(),
            new NavigateHandler(),
            new ParseArgumentsHandler(),
            new SetMultiModeHandler());
        // 保持最多 ListenerInstanceCount 个 namedPipeServer 待命，防止
        // 多个客户端瞬发连接不上（比如为 1 时接不住 Shell 里的接连两次请求）
        for (int i = 0; i < ListenerInstanceCount; i++)
        {
            _ = Task.Run(PipeListeningLoopAsync);
        }
    }

    public static void RegisterHandler(params IHandler[] newHandlers)
    {
        foreach (IHandler handler in newHandlers)
        {
            handlers[handler.Identity] = handler;
        }
    }

    private static async Task PipeListeningLoopAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            NamedPipeServerStream namedPipeServer = null;
            PipeOptions pipeServerOptions = PipeOptions.Asynchronous
                | PipeOptions.CurrentUserOnly;
            try
            {
                namedPipeServer = new NamedPipeServerStream(
                    PipeDiscovery.OwnPipeName,
                    PipeDirection.InOut,
                    ListenerInstanceCount,
                    PipeTransmissionMode.Message,
                    pipeServerOptions,
                    inBufferSize: InOutBufferSize,
                    outBufferSize: InOutBufferSize);
                await namedPipeServer.WaitForConnectionAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                namedPipeServer?.Dispose();
                return;
            }
            catch (Exception)
            {
                namedPipeServer?.Dispose();
                continue;
            }
            // 不等待完成，避免某个命令处理耗时期间无法接收后续命令
            _ = Task.Run(() => PipeHandleAsync(namedPipeServer));
        }
    }

    private static async Task PipeHandleAsync(NamedPipeServerStream server)
    {
        try
        {
            byte[] headerBytes = new byte[RequestHeader.Size];
            await server.ReadExactlyAsync(headerBytes, cts.Token);
            RequestHeader header = MemoryMarshal.Read<RequestHeader>(headerBytes);
            if (!header.IsValid)
            {
                return;
            }
            byte[] payload = new byte[header.PayloadBytes];
            if (payload.Length != 0)
            {
                await server.ReadExactlyAsync(payload, cts.Token);
            }
            HandlerResult handlerResult = await DispatchAsync(header.Identity, payload, cts.Token);
            await WriteResponseAsync(server, handlerResult);
        }
        catch (Exception)
        {
            // 客户端可能已断开，此时响应写不出去，丢弃即可
        }
        finally
        {
            await server.DisposeAsync();
        }
    }

    /// <summary>
    /// 分发到 UI 线程执行：所有命令最终都要操作窗口或表格，
    /// 不在入口统一编组的话，每个 Handler 都要各自处理线程切换，容易漏。
    /// 未知命令或 handler 抛异常时返回带对应状态、无数据段的响应。
    /// </summary>
    private static async Task<HandlerResult> DispatchAsync(
        HandlerIdentity identity, ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        if (!handlers.TryGetValue(identity, out IHandler handler))
        {
            return new HandlerResult { Status = HandlerStatus.BadCommand };
        }
        try
        {
            // 右至左两层 await 分别等待 DispatcherOperation 和 Task<HandlerResult>
            return await await Synchronization.UI.InvokeAsync(() => handler.HandleAsync(payload, token));
        }
        catch (Exception)
        {
            return new HandlerResult { Status = HandlerStatus.Error };
        }
    }

    /// <summary>
    /// 分两次写：先写固定 8 字节响应头，若带数据段再写第二段。
    /// 数据段不打包进头（头无法承载变长数据），由 PayloadBytes 说明其长度，
    /// 客户端据此决定是否再读一段，与请求方向 CommandServer 读取 Payload 的方式对称。
    /// </summary>
    private static async Task WriteResponseAsync(NamedPipeServerStream server, HandlerResult result)
    {
        byte[] handlerPayload = result.Payload ?? [];
        byte[] headerBytes = new byte[ResponseHeader.Size];
        MemoryMarshal.Write(
            headerBytes,
            new ResponseHeader()
            {
                Status = result.Status,
                PayloadBytes = (uint)handlerPayload.Length,
            });
        await server.WriteAsync(headerBytes);
        if (handlerPayload.Length != 0)
        {
            await server.WriteAsync(handlerPayload);
        }
    }
}
