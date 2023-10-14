using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialBlake2sp : AbsOfficialBlake2
    {
        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2sp_new();

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2_delete_state(IntPtr statePtr);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_init(IntPtr statePtr, ulong outlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(DllName.Blake2, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override ulong MaxOutputSize => 32;

        public override string NamePrefix => "BLAKE2sp";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2SP;

        public OfficialBlake2sp(ulong bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialBlake2sp(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2_delete_state(statePtr);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2sp_final(statePtr, output, outlen);
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2sp_init(statePtr, outlen);
        }

        public override IntPtr Blake2New()
        {
            return blake2sp_new();
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2sp_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2sp_update(statePtr, ref input, inlen);
        }
    }
}
