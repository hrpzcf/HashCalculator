using System;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator.IPC.Handlers;

/// <summary>
/// 查询本实例当前是否处于多实例模式。
/// 响应数据段为单字节 0/1，与 C/C++ 的 bool 语义一致（0 假 1 真），接收方以 data[0] != 0 判断。
/// </summary>
internal sealed class GetMultiModeHandler : IHandler
{
    public HandlerIdentity Identity => HandlerIdentity.GetAppMultiMode;

    public Task<HandlerResult> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        byte value = Convert.ToByte(Settings.Current.RunInMultiInstMode);
        return Task.FromResult(HandlerResult.OfPayload([value]));
    }
}
