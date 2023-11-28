using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class GmSslSM3 : HashAlgorithm, IHashAlgoInfo
    {
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sm3_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sm3_delete(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sm3_init(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sm3_update(IntPtr state, byte[] input, ulong size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sm3_update(IntPtr state, ref byte input, ulong size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sm3_final(IntPtr state, byte[] output);

        public int DigestLength => 32;

        public string AlgoName => "SM3";

        public AlgoType AlgoType => AlgoType.SM3;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                sm3_delete(this._state);
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
            this._state = sm3_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            sm3_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new GmSslSM3();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                sm3_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                sm3_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            sm3_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
