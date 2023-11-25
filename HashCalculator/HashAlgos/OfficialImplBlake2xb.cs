using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2xb : OfficialImplBlake2
    {
        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2xb_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2xb_delete(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_init(IntPtr statePtr, ulong outlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_final(IntPtr statePtr, byte[] output, ulong outlen);

        // 最大摘要长度本是0xFFFFFFFF，但此处受int最大值限制故设为int.MaxValue
        public override int MaxOutputSize => int.MaxValue;

        public override string NamePrefix => "BLAKE2xb";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2XB;

        public OfficialImplBlake2xb(int bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2xb(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2xb_delete(statePtr);
        }

        public override IntPtr Blake2New()
        {
            return blake2xb_new();
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2xb_init(statePtr, outlen);
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2xb_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2xb_update(statePtr, ref input, inlen);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2xb_final(statePtr, output, outlen);
        }
    }
}
