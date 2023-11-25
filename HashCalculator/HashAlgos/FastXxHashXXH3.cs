﻿using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    /// <summary>
    /// xxhash3-64
    /// </summary>
    internal class FastXxHashXXH3 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXH_errorcode _errorCode = XXH_errorcode.XXH_OK;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xxh3_64_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh3_64_init(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh3_64_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh3_64_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong xxh3_64_final(IntPtr statePtr);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh3_64_delete(IntPtr statePtr);

        public int DigestLength => 8;

        public string AlgoName => "XXH3";

        public AlgoType AlgoType => AlgoType.XXHASH3;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                xxh3_64_delete(this._state);
                this._state = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.DeleteState();
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            this._errorCode = XXH_errorcode.XXH_OK;
            this.DeleteState();
            this._state = xxh3_64_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = xxh3_64_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH3();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode == XXH_errorcode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart != 0 || cbSize != array.Length)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = xxh3_64_update(this._state, ref input, (ulong)cbSize);
            }
            else
            {
                this._errorCode = xxh3_64_update(this._state, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode == XXH_errorcode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            ulong hashResult = xxh3_64_final(this._state);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
