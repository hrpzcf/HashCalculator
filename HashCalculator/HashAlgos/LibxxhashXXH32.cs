using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibXxHashXXH32 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr statePtr = IntPtr.Zero;
        private XXH_errorcode error = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH32";

        public AlgoType AlgoType => AlgoType.XXHASH32;

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH32_createState();

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_freeState(IntPtr statePtr);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint XXH32_digest(IntPtr statePtr);

        [DllImport("xxhash.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_reset(IntPtr statePtr, uint seed);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH32_freeState(this.statePtr);
                this.statePtr = IntPtr.Zero;
            }
        }

        public override void Initialize()
        {
            this.statePtr = XXH32_createState();
            if (this.statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH32_reset(this.statePtr, 0);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibXxHashXXH32();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this.error != XXH_errorcode.XXH_ERROR && this.statePtr != IntPtr.Zero)
            {
                if (ibStart != 0 || cbSize != array.Length)
                {
                    array = array.Skip(ibStart).Take(cbSize).ToArray();
                }
                this.error = XXH32_update(this.statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this.error == XXH_errorcode.XXH_ERROR || this.statePtr == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }
            uint hashResult = XXH32_digest(this.statePtr);
            byte[] hashBytes = BitConverter.GetBytes(hashResult);
            Array.Reverse(hashBytes);
            return hashBytes;
        }
    }
}
