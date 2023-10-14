using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialBlake2xb : AbsOfficialBlake2
    {
        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2xb_new();

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2_delete_state(IntPtr statePtr);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_init(IntPtr statePtr, ulong outlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xb_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override ulong MaxOutputSize => 0xffffffffUL;

        public override string NamePrefix => "BLAKE2xb";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2XB;

        public OfficialBlake2xb(ulong bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialBlake2xb(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2_delete_state(statePtr);
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
