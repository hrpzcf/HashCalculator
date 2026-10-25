using System;
using System.Runtime.InteropServices;

namespace HashCalculator;

internal class OfficialBlake2b : OfficialBlake2
{
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr blake2b_new();

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake2b_delete(IntPtr statePtr);

    // 原生函数 length 类型是 size_t，对应 C# 的 nuint（x86 上 4 字节、x64 上 8 字节）
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2b_init(IntPtr statePtr, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2b_update(IntPtr statePtr, byte[] input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2b_update(IntPtr statePtr, ref byte input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2b_final(IntPtr statePtr, byte[] output, nuint length);

    public override int MaxOutputLength => 64;

    public override string NamePrefix => "Blake2b";

    public OfficialBlake2b(int bitLength) : base(bitLength) { }

    public override IHashAlgoInfo NewInstance()
    {
        return new OfficialBlake2b(this.bitLength);
    }

    public override void Blake2DeleteState(IntPtr statePtr)
    {
        blake2b_delete(statePtr);
    }

    public override IntPtr Blake2New()
    {
        return blake2b_new();
    }

    public override int Blake2Init(IntPtr statePtr, nuint length)
    {
        return blake2b_init(statePtr, length);
    }

    public override int Blake2Update(IntPtr statePtr, byte[] input, nuint length)
    {
        return blake2b_update(statePtr, input, length);
    }

    public override int Blake2Update(IntPtr statePtr, ref byte input, nuint length)
    {
        return blake2b_update(statePtr, ref input, length);
    }

    public override int Blake2Final(IntPtr statePtr, byte[] output, nuint length)
    {
        return blake2b_final(statePtr, output, length);
    }
}
