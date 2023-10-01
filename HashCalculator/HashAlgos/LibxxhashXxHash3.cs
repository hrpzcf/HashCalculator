using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    /// <summary>
    /// XxHash3-64
    /// </summary>
    internal class LibxxhashXxHash3 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr statePtr = IntPtr.Zero;
        private XXH_errorcode error = XXH_errorcode.XXH_OK;

        public string AlgoName => "XxHash3-64";

        public AlgoType AlgoType => AlgoType.XXHASH3_64;

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH3_createState();

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_freeState(IntPtr statePtr);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_64bits_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong XXH3_64bits_digest(IntPtr statePtr);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_64bits_reset(IntPtr statePtr);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH3_freeState(this.statePtr);
                this.statePtr = IntPtr.Zero;
            }
        }

        public override void Initialize()
        {
            this.statePtr = XXH3_createState();
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH3_64bits_reset(this.statePtr);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibxxhashXxHash3();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this.error != XXH_errorcode.XXH_ERROR && this.statePtr != IntPtr.Zero)
            {
                if (ibStart != 0 || cbSize != array.Length)
                {
                    array = array.Skip(ibStart).Take(cbSize).ToArray();
                }
                this.error = XXH3_64bits_update(this.statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this.error == XXH_errorcode.XXH_ERROR || this.statePtr == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }
            ulong hashResult = XXH3_64bits_digest(this.statePtr);
            byte[] hashBytes = BitConverter.GetBytes(hashResult);
            Array.Reverse(hashBytes);
            return hashBytes;
        }
    }
}
