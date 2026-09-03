using System;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.Views.Windows;

namespace HashCalculator.IPC.Handlers;

/// <summary>
/// 置前台命令：让本进程的主窗口显示并激活。
/// 由目标实例自己执行而非调用方去 SetForegroundWindow，
/// 因为目标窗口隐藏到托盘时 MainWindowHandle 为 0，外部无法激活，
/// 只有它自己能 Show() 并恢复最小化前状态。
/// </summary>
internal sealed class ActivateAppHandler : ICommandHandler
{
    public IPCMessageKind Kind => IPCMessageKind.Activate;

    public Task<CommandResponse> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        MainWindow.Current?.EnsureWindowIsShownAndActivated();
        return Task.FromResult(CommandResponse.Ok);
    }
}
