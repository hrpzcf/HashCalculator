using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator;

internal class OfficialBlake3 : HashAlgorithm, IHashAlgoInfo
{
    private readonly int bitLength;
    private AlgoType algoType = AlgoType.UNKNOWN;
    private IntPtr _state = IntPtr.Zero;

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr blake3_new();

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake3_delete(IntPtr state);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake3_init(IntPtr state);

    // 原生函数 length 类型是 size_t，对应 C# 的 nuint（x86 上 4 字节、x64 上 8 字节）
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake3_update(IntPtr state, byte[] input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake3_update(IntPtr state, ref byte input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake3_final(IntPtr state, byte[] output, nuint length);

    public int DigestLength { get; }

    public string AlgoName => $"Blake3-{this.bitLength}";

    public AlgoType AlgoType
    {
        get
        {
            if (this.algoType == AlgoType.UNKNOWN &&
                Enum.TryParse($"Blake3_{this.bitLength}", true, out AlgoType algo))
            {
                this.algoType = algo;
            }
            return this.algoType;
        }
    }

    private void DeleteState()
    {
        if (this._state != IntPtr.Zero)
        {
            blake3_delete(this._state);
            this._state = IntPtr.Zero;
        }
    }

    protected override void Dispose(bool disposing)
    {
        this.DeleteState();
        base.Dispose(disposing);
    }

    public OfficialBlake3(int bitLength)
    {
        if (bitLength < 8 || bitLength % 8 != 0)
        {
            throw new ArgumentException($"Invalid bit length");
        }
        this.bitLength = bitLength;
        this.DigestLength = bitLength / 8;
    }

    public override void Initialize()
    {
        this.DeleteState();
        this._state = blake3_new();
        if (this._state == IntPtr.Zero)
        {
            throw new Exception("Initialization failed");
        }
        blake3_init(this._state);
    }

    public void Release() => this.DeleteState();

    public IHashAlgoInfo NewInstance()
    {
        return new OfficialBlake3(this.bitLength);
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        if (this._state == IntPtr.Zero)
        {
            throw new InvalidOperationException("Not initialized yet");
        }
        if (ibStart == 0 && cbSize == array.Length)
        {
            blake3_update(this._state, array, (nuint)cbSize);
        }
        else
        {
            ReadOnlySpan<byte> sp = new ReadOnlySpan<byte>(array, ibStart, cbSize);
            ref byte input = ref MemoryMarshal.GetReference(sp);
            blake3_update(this._state, ref input, (nuint)cbSize);
        }
    }

    protected override byte[] HashFinal()
    {
        if (this._state == IntPtr.Zero)
        {
            throw new InvalidOperationException("Not initialized yet");
        }
        byte[] resultBuffer = new byte[this.DigestLength];
        blake3_final(this._state, resultBuffer, (nuint)this.DigestLength);
        return resultBuffer;
    }
}
