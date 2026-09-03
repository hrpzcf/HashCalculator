using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HashCalculator.IPC;

/// <summary>
/// 定长消息头，与 Shell 扩展（C++）侧的 IPC_HEADER 布局严格一致。
/// Pack = 1 不可省略：C++ 侧用 #pragma pack(1)，若此处不指定，
/// 两端字段之间插入的填充字节可能不同，导致解析出的长度和命令全部错位。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IPCMessageHeader
{
    public const uint CurrentVersion = 1;

    /// <summary>单次消息允许的最大 Payload 字节数，防止损坏的长度字段导致超大内存分配</summary>
    public const int MaxPayloadBytes = 8 * 1024 * 1024;

    public readonly bool IsValid => this.Version == CurrentVersion
        && this.PayloadBytes <= MaxPayloadBytes;

    public uint Version;

    /// <summary><see cref="IPCMessageKind"/> 的值</summary>
    public uint Kind;

    /// <summary>Payload 的字节数，0 表示无 Payload</summary>
    public uint PayloadBytes;

    public uint SourcePid;

    public static int Size => Unsafe.SizeOf<IPCMessageHeader>();
}

/// <summary>
/// 响应头，同样与 C++ 侧布局一致。
/// PayloadBytes 为 0 时后面不跟数据段，客户端据此决定是否再读一段。
/// 数据段字节紧跟在本头之后，长度由 PayloadBytes 说明，与请求方向对称。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct IPCResponseHeader
{
    /// <summary><see cref="IPCMessageStatus"/> 的值</summary>
    public uint Status;

    /// <summary>响应附带数据段的字节数，0 表示无数据段</summary>
    public uint PayloadBytes;

    public static int Size => Unsafe.SizeOf<IPCResponseHeader>();
}
