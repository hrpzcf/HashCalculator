using System;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.Views.Windows;

namespace HashCalculator.IPC.Handlers;

/// <summary>
/// 执行一条待处理的命令行参数（compute/verify 等）。<br/>
/// Payload 为 Shell 扩展发送的参数数组（ANSI），多个参数以 \0 分隔，
/// 首项是真实 verb（compute/verify），不含可执行名占位符 CreateProcessA 路径特有的占位符不在此传输）。<br/>
/// 接收方按 \0 拆分为参数数组后，交给<see cref="MainWindow.HandleReceivedCommandLine"/>，
/// 与命令行启动走同一套处理逻辑。<br/>
/// 用参数数组而非整条命令行，是因为各参数在 Shell 侧已是独立 token，无需再按引号/空格规则解析，
/// 也避免路径含空格时的拆分歧义。
/// </summary>
internal sealed class ParseArgumentsHandler : ICommandHandler
{
    public IPCMessageKind Kind => IPCMessageKind.ParseArguments;

    public Task<CommandResponse> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        string[] arguments = IPCPayloadCodecs.Decode(payload).Split('\0', StringSplitOptions.RemoveEmptyEntries);
        MainWindow.Current?.HandleReceivedCommandLine(arguments);
        return Task.FromResult(CommandResponse.Ok);
    }
}
