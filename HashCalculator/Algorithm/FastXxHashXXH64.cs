using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class FastXxHashXXH64 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXHErrorCode _errorCode = XXHErrorCode.XXH_OK;

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xxh64_new();

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh64_init(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh64_update(IntPtr state, byte[] input, ulong length);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh64_update(IntPtr state, ref byte input, ulong length);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong xxh64_final(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXHErrorCode xxh64_delete(IntPtr state);

        public int DigestLength => 8;

        public string AlgoName => "XXH64";

        public AlgoType AlgoType => AlgoType.XXH64;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                xxh64_delete(this._state);
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
            this._errorCode = XXHErrorCode.XXH_OK;
            this.DeleteState();
            this._state = xxh64_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = xxh64_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH64();
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
            if (ibStart == 0 && cbSize == array.Length)
            {
                this._errorCode = xxh64_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = xxh64_update(this._state, ref input, (ulong)cbSize);
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
            ulong hashResult = xxh64_final(this._state);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
