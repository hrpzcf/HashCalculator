using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2bp : OfficialImplBlake2
    {
        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2bp_new();

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2bp_delete(IntPtr statePtr);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2bp_init(IntPtr statePtr, ulong outlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2bp_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2bp_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2bp_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override int MaxOutputLength => 64;

        public override string NamePrefix => "BLAKE2bp";

        public OfficialImplBlake2bp(int bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2bp(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2bp_delete(statePtr);
        }

        public override IntPtr Blake2New()
        {
            return blake2bp_new();
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2bp_init(statePtr, outlen);
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2bp_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2bp_update(statePtr, ref input, inlen);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2bp_final(statePtr, output, outlen);
        }
    }
}
