using System;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator.IPC;

internal interface ICommandHandler
{
    IPCMessageKind Kind { get; }

    /// <summary>
    /// 由 <see cref="CommandExecutor"/> 保证在 UI 线程调用，实现类可直接操作窗口与表格。
    /// 返回 <see cref="CommandResponse"/>，其中 Payload 用于查询类命令回传数据。
    /// </summary>
    Task<CommandResponse> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token);
}
