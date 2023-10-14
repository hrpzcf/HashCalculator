using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class HashratWhirlpool : HashAlgorithm, IHashAlgoInfo
    {
        private const int outputSize = 64;
        private IntPtr _statePtr = IntPtr.Zero;

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr whirlpool_new();

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_delete(IntPtr statePtr);

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern int whirlpool_init(IntPtr statePtr);

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern int whirlpool_update(IntPtr statePtr, byte[] input, ulong inlen);

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern int whirlpool_update(IntPtr statePtr, ref byte input, ulong inlen);

        [DllImport(DllName.Whirlpool, CallingConvention = CallingConvention.Cdecl)]
        private static extern int whirlpool_final(IntPtr statePtr, byte[] output, ulong outlen);

        public string AlgoName => "Whirlpool";

        public AlgoType AlgoType => AlgoType.WHIRLPOOL;

        public override void Initialize()
        {
            this._statePtr = whirlpool_new();
            if (this._statePtr == IntPtr.Zero)
            {
                throw new NullReferenceException("Initialization failed");
            }
            else
            {
                whirlpool_init(this._statePtr);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new HashratWhirlpool();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                whirlpool_update(this._statePtr, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                whirlpool_update(this._statePtr, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[outputSize];
            whirlpool_final(this._statePtr, resultBuffer, outputSize);
            return resultBuffer;
        }
    }
}
