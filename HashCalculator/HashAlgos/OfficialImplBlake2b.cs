using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2b : OfficialImplBlake2
    {
        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2b_new();

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2_delete(IntPtr statePtr);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2b_init(IntPtr statePtr, ulong outlen);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2b_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2b_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2b_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override ulong MaxOutputSize => 64;

        public override string NamePrefix => "BLAKE2b";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2B;

        public OfficialImplBlake2b(ulong bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2b(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2_delete(statePtr);
        }

        public override IntPtr Blake2New()
        {
            return blake2b_new();
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2b_init(statePtr, outlen);
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2b_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2b_update(statePtr, ref input, inlen);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2b_final(statePtr, output, outlen);
        }
    }
}
