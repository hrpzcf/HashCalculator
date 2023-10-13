using System;
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
        private IntPtr _statePtr = IntPtr.Zero;
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
        private static extern XXH_errorcode XXH3_128bits_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH128Hash XXH3_128bits_digest(IntPtr statePtr);

        [DllImport(DllName.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_reset(IntPtr statePtr);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this._statePtr != IntPtr.Zero)
            {
                XXH_errorcode _ = XXH3_freeState(this._statePtr);
                this._statePtr = IntPtr.Zero;
            }
        }

        public override void Initialize()
        {
            this._statePtr = XXH3_createState();
            if (this._statePtr == IntPtr.Zero)
            {
                throw new NullReferenceException("Initialization failed");
            }
            XXH_errorcode _ = XXH3_128bits_reset(this._statePtr);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibXxHashXXH128();
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
                this.error = XXH3_128bits_update(this._statePtr, ref input, (ulong)cbSize);
            }
            else
            {
                this.error = XXH3_128bits_update(this._statePtr, array, (ulong)cbSize);
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
            XXH128Hash hashResult = XXH3_128bits_digest(this._statePtr);
            byte[] hashBytesLow = BitConverter.GetBytes(hashResult.low64);
            byte[] hashBytesHigh = BitConverter.GetBytes(hashResult.high64);
            Array.Reverse(hashBytesLow);
            Array.Reverse(hashBytesHigh);
            byte[] resultBuffer = new byte[hashBytesLow.Length + hashBytesHigh.Length];
            Array.Copy(hashBytesHigh, resultBuffer, hashBytesHigh.Length);
            Array.Copy(hashBytesLow, 0, resultBuffer, hashBytesHigh.Length, hashBytesLow.Length);
            return resultBuffer;
        }
    }
}
