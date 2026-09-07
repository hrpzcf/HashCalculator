using System;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator.IPC.Handlers;

internal interface IHandler
{
    HandlerIdentity Identity { get; }

    /// <summary>
    /// 由 <see cref="PipeCommandHost.DispatchAsync"/> 保证在 UI 线程调用。<br/>
    /// 返回 <see cref="HandlerResult"/>，其中 Payload 成员用于查询类命令回传数据。
    /// </summary>
    Task<HandlerResult> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token);
}
