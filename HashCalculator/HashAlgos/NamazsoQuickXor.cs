using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NamazsoQuickXor : HashAlgorithm, IHashAlgoInfo
    {
        private const int _size = 20;
        private IntPtr _state = IntPtr.Zero;

        public string AlgoName => "QuickXor";

        public AlgoType AlgoType => AlgoType.QUICKXOR;

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr qxhash_new();

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern void qxhash_delete(IntPtr state);

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern void qxhash_init(IntPtr state);

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern void qxhash_update(IntPtr state, byte[] input, ulong size);

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern void qxhash_update(IntPtr state, ref byte input, ulong size);

        [DllImport(DllName.QuickXor, CallingConvention = CallingConvention.Cdecl)]
        private static extern void qxhash_final(IntPtr state, byte[] output);

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                qxhash_delete(this._state);
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
            this._state = qxhash_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            qxhash_init(this._state);
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
                qxhash_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> sp = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(sp);
                qxhash_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[_size];
            qxhash_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
