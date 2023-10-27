using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class FastXxHashXXH32 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXH_errorcode _errorCode = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH32";

        public AlgoType AlgoType => AlgoType.XXHASH32;

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr xxh32_new();

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh32_delete(IntPtr statePtr);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh32_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh32_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint xxh32_final(IntPtr statePtr);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode xxh32_init(IntPtr statePtr);

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                xxh32_delete(this._state);
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
            this._state = xxh32_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = xxh32_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH32();
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
            if (ibStart == 0 && cbSize == array.Length)
            {
                this._errorCode = xxh32_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = xxh32_update(this._state, ref input, (ulong)cbSize);
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
            uint hashResult = xxh32_final(this._state);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
