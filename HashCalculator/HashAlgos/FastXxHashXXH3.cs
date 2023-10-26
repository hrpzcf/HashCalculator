using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    /// <summary>
    /// xxhash3-64
    /// </summary>
    internal class FastXxHashXXH3 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;
        private XXH_errorcode _errorCode = XXH_errorcode.XXH_OK;

        public string AlgoName => "XXH3";

        public AlgoType AlgoType => AlgoType.XXHASH3;

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr XXH3_createState();

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_freeState(IntPtr statePtr);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_64bits_update(IntPtr statePtr, byte[] input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_64bits_update(IntPtr statePtr, ref byte input, ulong length);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong XXH3_64bits_digest(IntPtr statePtr);

        [DllImport(Embedded.XxHash, CallingConvention = CallingConvention.Cdecl)]
        private static extern XXH_errorcode XXH3_64bits_reset(IntPtr statePtr);

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
            this._errorCode = XXH_errorcode.XXH_OK;
            this.DeleteState();
            this._state = XXH3_createState();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            this._errorCode = XXH3_64bits_reset(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new FastXxHashXXH3();
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
                this._errorCode = XXH3_64bits_update(this._state, ref input, (ulong)cbSize);
            }
            else
            {
                this._errorCode = XXH3_64bits_update(this._state, array, (ulong)cbSize);
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
            ulong hashResult = XXH3_64bits_digest(this._state);
            byte[] resultBuffer = BitConverter.GetBytes(hashResult);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
