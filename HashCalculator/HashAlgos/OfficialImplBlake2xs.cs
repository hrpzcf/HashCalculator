﻿using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2xs : OfficialImplBlake2
    {
        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2xs_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2xs_delete(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xs_init(IntPtr statePtr, ulong outlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xs_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xs_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2xs_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override int MaxOutputSize => 0xFFFF;

        public override string NamePrefix => "BLAKE2xs";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2XS;

        public OfficialImplBlake2xs(int bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2xs(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2xs_delete(statePtr);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2xs_final(statePtr, output, outlen);
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2xs_init(statePtr, outlen);
        }

        public override IntPtr Blake2New()
        {
            return blake2xs_new();
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2xs_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2xs_update(statePtr, ref input, inlen);
        }
    }
}
