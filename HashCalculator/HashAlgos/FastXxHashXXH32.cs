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

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH32_createState();

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_freeState(IntPtr statePtr);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint XXH32_digest(IntPtr statePtr);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH32_reset(IntPtr statePtr, uint seed);

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                XXH32_freeState(this._state);
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
            this._state = XXH32_createState();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = XXH32_reset(this._state, 0);
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
                this._errorCode = XXH32_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = XXH32_update(this._state, ref input, (ulong)cbSize);
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
            uint hashResult = XXH32_digest(this._state);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
