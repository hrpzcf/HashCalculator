using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XXH128Hash
    {
        public ulong low64;
        public ulong high64;
    }

    /// <summary>
    /// xxhash3-128
    /// </summary>
    internal class LibXxHashXXH128 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr statePtr = IntPtr.Zero;
        private XXH_errorcode error = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH128";

        public AlgoType AlgoType => AlgoType.XXHASH128;

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH3_createState();

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_freeState(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH128Hash XXH3_128bits_digest(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_reset(IntPtr statePtr);

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
                XXH_errorcode _ = XXH3_128bits_reset(this.statePtr);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibXxHashXXH128();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this.error != XXH_errorcode.XXH_ERROR && this.statePtr != IntPtr.Zero)
            {
                if (ibStart != 0 || cbSize != array.Length)
                {
                    array = array.Skip(ibStart).Take(cbSize).ToArray();
                }
                this.error = XXH3_128bits_update(this.statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this.error == XXH_errorcode.XXH_ERROR || this.statePtr == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }
            XXH128Hash hashResult = XXH3_128bits_digest(this.statePtr);
            byte[] hashBytesLow = BitConverter.GetBytes(hashResult.low64);
            byte[] hashBytesHigh = BitConverter.GetBytes(hashResult.high64);
            Array.Reverse(hashBytesLow);
            Array.Reverse(hashBytesHigh);
            byte[] hashBytesFinal = new byte[hashBytesLow.Length + hashBytesHigh.Length];
            Array.Copy(hashBytesHigh, hashBytesFinal, hashBytesHigh.Length);
            Array.Copy(hashBytesLow, 0, hashBytesFinal, hashBytesHigh.Length, hashBytesLow.Length);
            return hashBytesFinal;
        }
    }
}
