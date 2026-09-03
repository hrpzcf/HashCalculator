using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.Others;

namespace HashCalculator.IPC;

/// <summary>
/// 跨进程命令的接收分发器：把管道收到的命令分发到对应的 Handler。
/// 本进程内的直接操作不经过它，由调用方直接调用 MainWindow/ViewModel 完成。
/// </summary>
internal sealed class CommandExecutor
{
    private readonly Dictionary<IPCMessageKind, ICommandHandler> handlers = [];

    public CommandExecutor Register(params ICommandHandler[] newHandlers)
    {
        foreach (ICommandHandler handler in newHandlers)
        {
            this.handlers[handler.Kind] = handler;
        }
        return this;
    }

    /// <summary>
    /// 分发到 UI 线程执行：所有命令最终都要操作窗口或表格，
    /// 不在入口统一编组的话，每个 Handler 都要各自处理线程切换，容易漏。
    /// 未知命令或 handler 抛异常时返回带对应状态、无数据段的响应。
    /// </summary>
    public async Task<CommandResponse> DispatchAsync(IPCMessageKind kind, ReadOnlyMemory<byte> payload,
        CancellationToken token)
    {
        if (!this.handlers.TryGetValue(kind, out ICommandHandler handler))
        {
            return new CommandResponse { Status = IPCMessageStatus.UnknownCommand };
        }
        try
        {
            Task<CommandResponse> task = await Synchronization.UI.InvokeAsync(() => handler.HandleAsync(payload, token));
            return await task;
        }
        catch (Exception)
        {
            return new CommandResponse { Status = IPCMessageStatus.Error };
        }
    }
}
