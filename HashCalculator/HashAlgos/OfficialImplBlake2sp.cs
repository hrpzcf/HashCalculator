using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2sp : OfficialImplBlake2
    {
        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2sp_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2sp_delete(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_init(IntPtr statePtr, ulong outlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2sp_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override ulong MaxOutputSize => 32;

        public override string NamePrefix => "BLAKE2sp";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2SP;

        public OfficialImplBlake2sp(ulong bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2sp(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2sp_delete(statePtr);
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
