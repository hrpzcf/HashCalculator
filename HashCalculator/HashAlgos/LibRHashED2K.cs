using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class LibRHashED2K : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ed2k_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ed2k_delete(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ed2k_init(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ed2k_update(IntPtr state, byte[] input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ed2k_update(IntPtr state, ref byte input, ulong inlen);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ed2k_final(IntPtr state, byte[] output);

        public string AlgoName => "eD2k";

        public AlgoType AlgoType => AlgoType.ED2K;

        public int DigestLength => 16;

        private void DeleState()
        {
            if (this._state != IntPtr.Zero)
            {
                ed2k_delete(this._state);
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
            this._state = ed2k_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            ed2k_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new LibRHashED2K();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                ed2k_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                ed2k_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            ed2k_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
