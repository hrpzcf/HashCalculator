using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibXxHashXXH64 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _statePtr = IntPtr.Zero;
        private XXH_errorcode error = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH64";

        public AlgoType AlgoType => AlgoType.XXHASH64;

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH64_createState();

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH64_freeState(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH64_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH64_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong XXH64_digest(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH64_reset(IntPtr statePtr, ulong seed);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this._statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH64_freeState(this._statePtr);
                this._statePtr = IntPtr.Zero;
            }
        }

        public override void Initialize()
        {
            this._statePtr = XXH64_createState();
            if (this._statePtr == IntPtr.Zero)
            {
                throw new NullReferenceException("Initialization failed");
            }
            XXH_errorcode _ = XXH64_reset(this._statePtr, 0);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibXxHashXXH64();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this.error == XXH_errorcode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart != 0 || cbSize != array.Length)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this.error = XXH64_update(this._statePtr, ref input, (ulong)cbSize);
            }
            else
            {
                this.error = XXH64_update(this._statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this.error == XXH_errorcode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            ulong hashResult = XXH64_digest(this._statePtr);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
