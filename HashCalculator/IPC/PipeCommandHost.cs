using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.IPC.Handlers;
using HashCalculator.Others;
using Microsoft.Extensions.Logging;

namespace HashCalculator.IPC;

/// <summary>
/// 跨进程管道命令的宿主，每个 HashCaclulator 实例监听一条以自己进程 ID 命名的管道。<br/>
/// 常驻 ListenerLoopCount 条并行的监听循环，每条循环长期持有并复用同一条服务实例
/// （等连接 → 处理 → Disconnect → 再等连接）：处理期间该实例不接新连接，故常驻多条循环互相兜底，
/// 接得住 Shell 扩展接连发起的多条请求。
/// </summary>
internal static class PipeCommandHost
{
    private const int DefaultTimeoutMs = 200;

    /// <summary>
    /// 常驻监听循环数。复用式下每条循环长期持有并复用同一条服务实例，
    /// 因此这个数既是一次可同时处理的请求数，也是该管道名的实例上限，两者必然相等。
    /// （旧实现每条连接都重建新实例，实例上限还须额外预留"在途处理"的数量，
    /// 否则重建必然撞满配额、抛 IOException 空转。）
    /// </summary>
    private const int ListenerLoopCount = 4;

    /// <summary>
    /// 创建服务实例失败后的重试间隔，避免失败时空转
    /// </summary>
    private const int ListenerRetryDelayMs = 50;

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
        // 常驻 ListenerLoopCount 条循环，各自持有并复用一条服务实例，
        // 某条正在处理请求时它不接新连接，其余循环继续兜底
        for (int i = 0; i < ListenerLoopCount; i++)
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
            NamedPipeServerStream pipeServer = await CreateServerAsync();
            if (pipeServer is null)
            {
                continue;
            }
            // 一条实例反复复用：等连接 → 处理 → Disconnect → 再等连接，
            // 直到它报废（等待或断开失败）才换新的一条
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await pipeServer.WaitForConnectionAsync(cts.Token);
                    await PipeHandleAsync(pipeServer);
                    pipeServer.Disconnect();
                }
                catch (OperationCanceledException)
                {
                    pipeServer.Dispose();
                    return;
                }
                catch (Exception exception)
                {
                    // App.Logger 在 _host.Start() 之后才赋值，而本循环就在 _host.Start() 期间启动
                    App.Logger?.LogError(exception, "命名管道服务实例报废，将重建");
                    pipeServer.Dispose();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 创建一条服务实例；失败时记录并等待一小段时间，返回 null 让调用方重试
    /// </summary>
    private static async Task<NamedPipeServerStream> CreateServerAsync()
    {
        try
        {
            return new NamedPipeServerStream(
                PipeDiscovery.OwnPipeName,
                PipeDirection.InOut,
                ListenerLoopCount,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                inBufferSize: InOutBufferSize,
                outBufferSize: InOutBufferSize);
        }
        catch (Exception exception)
        {
            App.Logger?.LogError(exception, "创建命名管道服务实例失败");
            await Task.Delay(ListenerRetryDelayMs);
            return null;
        }
    }

    /// <summary>
    /// 读取并处理一条请求。不释放 server：
    /// 正常返回后由调用方 Disconnect 并复用这同一条实例。
    /// </summary>
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
