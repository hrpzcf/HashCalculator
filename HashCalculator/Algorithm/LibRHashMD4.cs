using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibRHashMD4 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr md4_new();

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void md4_delete(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void md4_init(IntPtr state);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void md4_update(IntPtr state, byte[] input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void md4_update(IntPtr state, ref byte input, ulong inlen);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void md4_final(IntPtr state, byte[] output);

        public string AlgoName => "MD4";

        public AlgoType AlgoType => AlgoType.MD4;

        public int DigestLength => 16;

        private void DeleState()
        {
            if (this._state != IntPtr.Zero)
            {
                md4_delete(this._state);
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
            this._state = md4_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            md4_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibRHashMD4();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                md4_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                md4_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            md4_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
