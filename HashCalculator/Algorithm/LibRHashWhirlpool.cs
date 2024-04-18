using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibRHashWhirlpool : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr whirlpool_new();

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_delete(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_init(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_update(IntPtr state, byte[] input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_update(IntPtr state, ref byte input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void whirlpool_final(IntPtr state, byte[] output);

        public int DigestLength { get; } = 64;

        public string AlgoName => "Whirlpool";

        public AlgoType AlgoType => AlgoType.WHIRLPOOL;

        private void DeleState()
        {
            if (this._state != IntPtr.Zero)
            {
                whirlpool_delete(this._state);
                this._state = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.DeleState();
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            this.DeleState();
            this._state = whirlpool_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            whirlpool_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibRHashWhirlpool();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                whirlpool_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                whirlpool_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            whirlpool_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
