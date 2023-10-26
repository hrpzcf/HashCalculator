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
    internal class FastXxHashXXH128 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXH_errorcode _errorCode = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH128";

        public AlgoType AlgoType => AlgoType.XXHASH128;

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH3_createState();

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_freeState(IntPtr state);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_update(IntPtr state, byte[] input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_update(IntPtr state, ref byte input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH128Hash XXH3_128bits_digest(IntPtr state);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_128bits_reset(IntPtr state);

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                XXH3_freeState(this._state);
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
            this.DeleteState();
            this._state = XXH3_createState();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = XXH3_128bits_reset(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH128();
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
                this._errorCode = XXH3_128bits_update(this._state, ref input, (ulong)cbSize);
            }
            else
            {
                this._errorCode = XXH3_128bits_update(this._state, array, (ulong)cbSize);
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
            XXH128Hash hashResult = XXH3_128bits_digest(this._state);
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
