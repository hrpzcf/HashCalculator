using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator.IPC;

/// <summary>
/// 每个实例监听一条以自己进程 ID 命名的管道。
/// 只用一个监听循环：管道名唯一，不存在多个客户端争抢同一条管道的情况，
/// 因此不需要维持多个空闲实例。
/// </summary>
internal sealed class CommandServer : IDisposable
{
    private const int BufferSize = 64 * 1024;
    private readonly CommandExecutor executor;
    private readonly CancellationTokenSource cts = new();

    public CommandServer(CommandExecutor executor)
    {
        this.executor = executor;
    }

    public void Start()
    {
        // 它已捕获全部异常，不会把未观察异常抛给线程池。
        _ = Task.Run(this.ListeningLoopAsync);
    }

    public void Dispose()
    {
        // 只取消，不 Dispose：监听循环还要访问 cts.Token，
        // 而 Dispose 之后该属性会抛 ObjectDisposedException，
        // 它不是 OperationCanceledException，会被循环底部的 catch(Exception)
        // 吞掉并 continue，进而造成不断创建管道实例死循环。
        this.cts.Cancel();
    }

    private async Task ListeningLoopAsync()
    {
        while (!this.cts.IsCancellationRequested)
        {
            NamedPipeServerStream server = null;
            try
            {
                server = new NamedPipeServerStream(
                    InstanceDiscovery.OwnPipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                    inBufferSize: BufferSize,
                    outBufferSize: BufferSize);
                await server.WaitForConnectionAsync(this.cts.Token);
            }
            catch (OperationCanceledException)
            {
                server?.Dispose();
                return;
            }
            catch (Exception)
            {
                server?.Dispose();
                continue;
            }
            // 不等待处理完成，避免某个命令处理耗时期间无法接收后续命令
            _ = Task.Run(() => this.HandleAsync(server));
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
    /// 客户端据此决定是否再读一段，与请求方向 CommandServer 读取 payload 的方式对称。
    /// </summary>
    private static async Task WriteResponseAsync(NamedPipeServerStream server, CommandResponse response)
    {
        byte[] payload = response.Payload ?? [];
        byte[] headerBytes = new byte[IPCResponseHeader.Size];
        MemoryMarshal.Write(headerBytes, new IPCResponseHeader()
        {
            Status = (uint)response.Status,
            PayloadBytes = (uint)payload.Length,
        });
        await server.WriteAsync(headerBytes);
        if (payload.Length != 0)
        {
            await server.WriteAsync(payload);
        }
    }
}
