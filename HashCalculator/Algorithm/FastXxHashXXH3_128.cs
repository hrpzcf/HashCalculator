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

    internal class FastXxHashXXH3_128 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXHErrorCode _errorCode = XXHErrorCode.XXH_OK;

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xxh3_128_new();

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh3_128_init(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh3_128_update(IntPtr state, byte[] input, ulong length);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh3_128_update(IntPtr state, ref byte input, ulong length);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH128Hash xxh3_128_final(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh3_128_delete(IntPtr state);

        public int DigestLength => 16;

        public string AlgoName => "XXH3-128";

        public AlgoType AlgoType => AlgoType.XXH3_128;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                xxh3_128_delete(this._state);
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
            this._state = xxh3_128_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = xxh3_128_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH3_128();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode == XXHErrorCode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart != 0 || cbSize != array.Length)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = xxh3_128_update(this._state, ref input, (ulong)cbSize);
            }
            else
            {
                this._errorCode = xxh3_128_update(this._state, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode == XXHErrorCode.XXH_ERROR)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            XXH128Hash hashResult = xxh3_128_final(this._state);
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
