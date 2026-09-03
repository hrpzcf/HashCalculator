using System;
using System.Text;

namespace HashCalculator.IPC;

/// <summary>
/// 命令 Payload 的编码与解码约定，
/// 跨进程统一使用 ANSI（系统活动代码页）编码字符串。<br/>
/// Shell 扩展以 ANSI 编码的字节，C# 端用同一代码页的编码器解码即可还原，
/// 因此两端无需关心具体是哪个代码页。
/// </summary>
internal static class IPCPayloadCodecs
{
    // 带 ExceptionFallback 而非替换式 fallback：
    // 一旦遇到当前代码页无法表达的字符，立即抛异常暴露，
    // 避免静默替换产生乱码或解析错位而不自知。
    private static readonly Encoding ActiveCodePage =
        Encoding.GetEncoding(0, 
            new EncoderExceptionFallback(),
            new DecoderExceptionFallback());

    /// <summary>
    /// 把字符串编码为 Payload 字节（ANSI）
    /// </summary>
    public static byte[] Encode(string text)
    {
        return ActiveCodePage.GetBytes(text);
    }

    /// <summary>
    /// 把 Payload 字节解码为字符串（ANSI）
    /// </summary>
    public static string Decode(ReadOnlyMemory<byte> bytes)
    {
        return ActiveCodePage.GetString(bytes.Span);
    }
}
