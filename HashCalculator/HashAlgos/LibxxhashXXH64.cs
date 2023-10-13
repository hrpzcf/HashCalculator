using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibXxHashXXH64 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr statePtr = IntPtr.Zero;
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
        private static extern ulong XXH64_digest(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH64_reset(IntPtr statePtr, ulong seed);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH64_freeState(this.statePtr);
                this.statePtr = IntPtr.Zero;
            }
        }

        public override void Initialize()
        {
            this.statePtr = XXH64_createState();
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH64_reset(this.statePtr, 0);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibXxHashXXH64();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this.error != XXH_errorcode.XXH_ERROR && this.statePtr != IntPtr.Zero)
            {
                if (ibStart != 0 || cbSize != array.Length)
                {
                    array = array.Skip(ibStart).Take(cbSize).ToArray();
                }
                this.error = XXH64_update(this.statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this.error == XXH_errorcode.XXH_ERROR || this.statePtr == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }
            ulong hashResult = XXH64_digest(this.statePtr);
            byte[] hashBytes = BitConverter.GetBytes(hashResult);
            Array.Reverse(hashBytes);
            return hashBytes;
        }
    }
}
