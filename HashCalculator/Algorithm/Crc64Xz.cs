using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator;

internal class Crc64Xz : HashAlgorithm, IHashAlgoInfo
{
    private ulong previousCrc64 = 0ul;

    // 原生函数 length 类型是 size_t，对应 C# 的 nuint（x86 上 4 字节、x64 上 8 字节）
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong crc64_update(byte[] input, nuint length, ulong prevCrc64);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong crc64_update(ref byte input, nuint length, ulong prevCrc64);

    public int DigestLength => 8;

    public string AlgoName => "CRC64";

    public AlgoType AlgoType => AlgoType.CRC64;

    public override void Initialize()
    {
        this.previousCrc64 = 0ul;
    }

    // 没有需要一次性释放的非托管资源，每次 Initialize 只把计数归零
    public void Release() { }

    public IHashAlgoInfo NewInstance()
    {
        return new Crc64Xz();
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (ibStart == 0 && cbSize == array.Length)
        {
            this.previousCrc64 = crc64_update(array, (nuint)cbSize, this.previousCrc64);
        }
        else
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
            ref byte input = ref MemoryMarshal.GetReference(span);
            this.previousCrc64 = crc64_update(ref input, (nuint)cbSize, this.previousCrc64);
        }
    }

    protected override byte[] HashFinal()
    {
        byte[] resultBuffer = BitConverter.GetBytes(this.previousCrc64);
        Array.Reverse(resultBuffer);
        return resultBuffer;
    }
}
