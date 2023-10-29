using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class OfficialImplBlake3 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private readonly int outputSize;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr _state = IntPtr.Zero;

        public string AlgoName => $"BLAKE3-{this.bitLength}";

        public AlgoType AlgoType
        {
            get
            {
                if (this.algoType == AlgoType.Unknown)
                {
                    if (Enum.TryParse($"BLAKE3_{this.bitLength}", true, out AlgoType algo))
                    {
                        this.algoType = algo;
                    }
                    else
                    {
                        this.algoType = AlgoType.BLAKE3;
                    }
                }
                return this.algoType;
            }
        }

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake3_new();

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_delete(IntPtr state);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_init(IntPtr state);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_update(IntPtr state, byte[] input, ulong size);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_update(IntPtr state, ref byte input, ulong size);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_final(IntPtr state, byte[] output, ulong size);

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                blake3_delete(this._state);
                this._state = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.DeleteState();
            base.Dispose(disposing);
        }

        public OfficialImplBlake3(int bitLength)
        {
            if (bitLength < 8 || bitLength % 8 != 0)
            {
                throw new ArgumentException($"Invalid bit length");
            }
            this.bitLength = bitLength;
            this.outputSize = bitLength / 8;
        }

        public override void Initialize()
        {
            this.DeleteState();
            this._state = blake3_new();
            if (this._state == IntPtr.Zero)
            {
                throw new Exception("Initialization failed");
            }
            blake3_init(this._state);
        }

        public IHashAlgoInfo NewInstance()
        {
            return new OfficialImplBlake3(this.bitLength);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                blake3_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> sp = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(sp);
                blake3_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.outputSize];
            blake3_final(this._state, resultBuffer, (ulong)this.outputSize);
            return resultBuffer;
        }
    }
}
