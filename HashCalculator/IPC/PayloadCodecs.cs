using System;
using System.Text;

namespace HashCalculator.IPC;

/// <summary>
/// 命令 Payload 的编码与解码约定，
/// 跨进程统一使用 UTF-16LE 编码字符串。<br/>
/// Shell 扩展以 UTF-16LE 编码的字节，C# 端用同一代码页的编码器解码即可还原，
/// 因此两端无需关心具体是哪个代码页。
/// </summary>
internal static class PayloadCodecs
{
    private static readonly Encoding PayloadEncoding = Encoding.Unicode;

    /// <summary>
    /// 把字符串编码为 Payload 字节（UTF-16LE）
    /// </summary>
    public static byte[] Encode(string text)
    {
        return PayloadEncoding.GetBytes(text);
    }

    /// <summary>
    /// 把 Payload 字节解码为字符串（UTF-16LE）
    /// </summary>
    public static string Decode(ReadOnlyMemory<byte> bytes)
    {
        return PayloadEncoding.GetString(bytes.Span);
    }
}
