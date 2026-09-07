using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HashCalculator.IPC;

/// <summary>
/// 响应头，同样与 C++ 侧布局一致。
/// PayloadBytes 为 0 时后面不跟数据段，客户端据此决定是否再读一段。
/// 数据段字节紧跟在本头之后，长度由 PayloadBytes 说明，与请求方向对称。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ResponseHeader
{
    /// <summary>
    /// <see cref="HandlerStatus"/> 的值
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public HandlerStatus Status;

    /// <summary>
    /// 响应附带数据段的字节数，0 表示无数据段
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public uint PayloadBytes;

    public static int Size => Unsafe.SizeOf<ResponseHeader>();
}
