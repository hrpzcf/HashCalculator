using System;
using System.Runtime.InteropServices;

namespace HashCalculator;

internal class OfficialBlake2bp : OfficialBlake2
{
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr blake2bp_new();

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern void blake2bp_delete(IntPtr statePtr);

    // 原生函数 length 类型是 size_t，对应 C# 的 nuint（x86 上 4 字节、x64 上 8 字节）
    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2bp_init(IntPtr statePtr, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2bp_update(IntPtr statePtr, byte[] input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2bp_update(IntPtr statePtr, ref byte input, nuint length);

    [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
    private static extern int blake2bp_final(IntPtr statePtr, byte[] output, nuint length);

    public override int MaxOutputLength => 64;

    public override string NamePrefix => "Blake2bp";

    public OfficialBlake2bp(int bitLength) : base(bitLength) { }

    public override IHashAlgoInfo NewInstance()
    {
        return new OfficialBlake2bp(this.bitLength);
    }

    public override void Blake2DeleteState(IntPtr statePtr)
    {
        blake2bp_delete(statePtr);
    }

    public override IntPtr Blake2New()
    {
        return blake2bp_new();
    }

    public override int Blake2Init(IntPtr statePtr, nuint length)
    {
        return blake2bp_init(statePtr, length);
    }

    public override int Blake2Update(IntPtr statePtr, byte[] input, nuint length)
    {
        return blake2bp_update(statePtr, input, length);
    }

    public override int Blake2Update(IntPtr statePtr, ref byte input, nuint length)
    {
        return blake2bp_update(statePtr, ref input, length);
    }

    public override int Blake2Final(IntPtr statePtr, byte[] output, nuint length)
    {
        return blake2bp_final(statePtr, output, length);
    }
}
