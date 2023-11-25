using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NamazsoQuickXor : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr quickxor_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void quickxor_delete(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void quickxor_init(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void quickxor_update(IntPtr state, byte[] input, ulong size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void quickxor_update(IntPtr state, ref byte input, ulong size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void quickxor_final(IntPtr state, byte[] output);

        public int DigestLength { get; } = 20;

        public string AlgoName => "QuickXor";

        public AlgoType AlgoType => AlgoType.QUICKXOR;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                quickxor_delete(this._state);
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
            this._state = quickxor_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            quickxor_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new NamazsoQuickXor();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                quickxor_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> sp = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(sp);
                quickxor_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            quickxor_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
