using System;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.Views.Windows;

namespace HashCalculator.IPC.Handlers;

/// <summary>
/// 更新本实例的多实例模式设置。
/// Payload 为单字节 0/1（0 单实例，1 多实例）。
/// 通过 MainWindow.ApplyAppMultiModeFromIPC 应用，使抑制标志生效，
/// 避免本实例因这次改动再次向其他实例广播，造成循环广播。
/// </summary>
internal sealed class SetMultiModeHandler : ICommandHandler
{
    public IPCMessageKind Kind => IPCMessageKind.SetAppMultiMode;

    public Task<CommandResponse> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        if (payload.Length != 1)
        {
            return Task.FromResult(new CommandResponse { Status = IPCMessageStatus.BadPayload });
        }
        MainWindow.Current?.ApplyAppMultiModeFromIPC(payload.Span[0] != 0);
        return Task.FromResult(CommandResponse.Ok);
    }
}
