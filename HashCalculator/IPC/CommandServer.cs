using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator.IPC;

/// <summary>
/// 每个实例监听一条以自己进程 ID 命名的管道。
/// 常驻 ListenerInstanceCount 个并行的监听循环：Shell 扩展常会连续快速发起多条请求，
/// 若只有一个监听循环，则在"accept 完一条、补建下一条 pipeServer"的空窗里接不住紧随其后的连接，
/// 故用多个常驻实例互相兜底。每个实例处理完请求后在 HandleAsync 里 DisposeAsync。
/// </summary>
internal sealed class CommandServer : IDisposable
{
    private const int InOutBufferSize = 64 * 1024;
    private const int ListenerInstanceCount = 2;
    private readonly CommandExecutor executor;
    private readonly CancellationTokenSource cts = new();

    public CommandServer(CommandExecutor executor)
    {
        this.executor = executor;
    }

    public void Start()
    {
        // 保持最多 ListenerInstanceCount 个 pipeServer 待命，防止
        // 多个客户端瞬发连接不上（比如为 1 时接不住 Shell 里的接连两次请求）
        for (int i = 0; i < ListenerInstanceCount; i++)
        {
            // 每个循环已捕获全部异常，不会把未观察异常抛给线程池
            _ = Task.Run(this.ListeningLoopAsync);
        }
    }

    public void Dispose()
    {
        // 只取消，不 Dispose：监听循环还要访问 cts.Token，
        // 而 Dispose 之后该属性会抛 ObjectDisposedException，
        // 它不是 OperationCanceledException，会被循环底部的 catch(Exception)
        // 吞掉并 continue，进而造成不断创建管道实例的死循环
        this.cts.Cancel();
    }

    private async Task ListeningLoopAsync()
    {
        while (!this.cts.IsCancellationRequested)
        {
            NamedPipeServerStream pipeServer = null;
            PipeOptions pipeServerOptions = PipeOptions.Asynchronous |
                PipeOptions.CurrentUserOnly;
            try
            {
                pipeServer = new NamedPipeServerStream(
                    InstanceDiscovery.OwnPipeName,
                    PipeDirection.InOut,
                    ListenerInstanceCount,
                    PipeTransmissionMode.Message,
                    pipeServerOptions,
                    inBufferSize: InOutBufferSize,
                    outBufferSize: InOutBufferSize);
                await pipeServer.WaitForConnectionAsync(this.cts.Token);
            }
            catch (OperationCanceledException)
            {
                pipeServer?.Dispose();
                return;
            }
            catch (Exception)
            {
                pipeServer?.Dispose();
                continue;
            }
            // 不等待处理完成，避免某个命令处理耗时期间无法接收后续命令
            _ = Task.Run(() => this.HandleAsync(pipeServer));
        }
    }

    private async Task HandleAsync(NamedPipeServerStream server)
    {
        try
        {
            byte[] headerBytes = new byte[IPCMessageHeader.Size];
            await server.ReadExactlyAsync(headerBytes, this.cts.Token);
            IPCMessageHeader header = MemoryMarshal.Read<IPCMessageHeader>(headerBytes);
            if (!header.IsValid)
            {
                return;
            }
            byte[] payload = new byte[header.PayloadBytes];
            if (payload.Length != 0)
            {
                await server.ReadExactlyAsync(payload, this.cts.Token);
            }
            CommandResponse response = await this.executor.DispatchAsync((IPCMessageKind)header.Kind,
                payload, this.cts.Token);
            await WriteResponseAsync(server, response);
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
    /// 分两次写：先写固定 8 字节响应头，若带数据段再写第二段。
    /// 数据段不打包进头（头无法承载变长数据），由 PayloadBytes 说明其长度，
    /// 客户端据此决定是否再读一段，与请求方向 CommandServer 读取 extraPayload 的方式对称。
    /// </summary>
    private static async Task WriteResponseAsync(NamedPipeServerStream server, CommandResponse response)
    {
        byte[] extraPayload = response.Payload ?? [];
        byte[] headerBytes = new byte[IPCResponseHeader.Size];
        MemoryMarshal.Write(headerBytes, new IPCResponseHeader()
        {
            Status = (uint)response.Status,
            PayloadBytes = (uint)extraPayload.Length,
        });
        await server.WriteAsync(headerBytes);
        if (extraPayload.Length != 0)
        {
            await server.WriteAsync(extraPayload);
        }
    }
}
