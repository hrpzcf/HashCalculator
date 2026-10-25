using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator;

internal class RHashWhirlpool : HashAlgorithm, IHashAlgoInfo
{
    private IntPtr _state = IntPtr.Zero;

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr whirlpool_new();

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whirlpool_delete(IntPtr state);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whirlpool_init(IntPtr state);

    // 原生函数 length 类型是 size_t，对应 C# 的 nuint（x86 上 4 字节、x64 上 8 字节）
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whirlpool_update(IntPtr state, byte[] input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whirlpool_update(IntPtr state, ref byte input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void whirlpool_final(IntPtr state, byte[] output);

    public int DigestLength { get; } = 64;

    public string AlgoName => "Whirlpool";

    public AlgoType AlgoType => AlgoType.WHIRLPOOL;

    private void DeleState()
    {
        if (this._state != IntPtr.Zero)
        {
            whirlpool_delete(this._state);
            this._state = IntPtr.Zero;
        }
    }

    protected override void Dispose(bool disposing)
    {
        this.DeleState();
        base.Dispose(disposing);
    }

    public override void Initialize()
    {
        this.DeleState();
        this._state = whirlpool_new();
        if (this._state == IntPtr.Zero)
        {
            throw new Exception("Initialization failed");
        }
        whirlpool_init(this._state);
    }

    public void Release() => this.DeleState();

    public IHashAlgoInfo NewInstance()
    {
        return new RHashWhirlpool();
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (this._state == IntPtr.Zero)
        {
            throw new InvalidOperationException("Not initialized yet");
        }
        if (ibStart == 0 && cbSize == array.Length)
        {
            whirlpool_update(this._state, array, (nuint)cbSize);
        }
        else
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
            ref byte input = ref MemoryMarshal.GetReference(span);
            whirlpool_update(this._state, ref input, (nuint)cbSize);
        }
    }

    protected override byte[] HashFinal()
    {
        if (this._state == IntPtr.Zero)
        {
            throw new InvalidOperationException("Not initialized yet");
        }
        byte[] resultBuffer = new byte[this.DigestLength];
        whirlpool_final(this._state, resultBuffer);
        return resultBuffer;
    }
}
