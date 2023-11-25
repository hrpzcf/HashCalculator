using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class HaclSha2Sha224 : HashAlgorithm, IHashAlgoInfo
    {
        private byte _errorCode = 0;
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sha224_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sha224_delete(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sha224_init(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte sha224_update(IntPtr state, byte[] input, uint size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte sha224_update(IntPtr state, ref byte input, uint size);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sha224_final(IntPtr state, byte[] output);

        public int DigestLength { get; } = 28;

        public string AlgoName => "SHA-224";

        public AlgoType AlgoType => AlgoType.SHA224;

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                sha224_delete(this._state);
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
            this._errorCode = 0;
            this._state = sha224_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            sha224_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new HaclSha2Sha224();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (this._errorCode > 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                this._errorCode = sha224_update(this._state, array, (uint)cbSize);
            }
            else
            {
                var span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = sha224_update(this._state, ref input, (uint)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (this._errorCode > 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            sha224_final(this._state, resultBuffer);
            return resultBuffer;
        }
    }
}
