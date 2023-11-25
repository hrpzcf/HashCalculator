﻿using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class OfficialImplBlake2s : OfficialImplBlake2
    {
        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake2s_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake2s_delete(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2s_init(IntPtr statePtr, ulong outlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2s_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2s_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int blake2s_final(IntPtr statePtr, byte[] output, ulong outlen);

        public override int MaxOutputSize => 32;

        public override string NamePrefix => "BLAKE2s";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2S;

        public OfficialImplBlake2s(int bitLength) : base(bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake2s(this.bitLength);
        }

        public override void Blake2DeleteState(IntPtr statePtr)
        {
            blake2s_delete(statePtr);
        }

        public override int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen)
        {
            return blake2s_final(statePtr, output, outlen);
        }

        public override int Blake2Init(IntPtr statePtr, ulong outlen)
        {
            return blake2s_init(statePtr, outlen);
        }

        public override IntPtr Blake2New()
        {
            return blake2s_new();
        }

        public override int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen)
        {
            return blake2s_update(statePtr, input, inlen);
        }

        public override int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen)
        {
            return blake2s_update(statePtr, ref input, inlen);
        }
    }
}
