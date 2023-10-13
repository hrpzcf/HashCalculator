using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class Blake3NetBlake3 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private readonly int outputSize;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr _hasher = IntPtr.Zero;
        private const int defaultOutputSize = 32;

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

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr blake3_new();

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_delete(IntPtr hasher);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_reset(IntPtr hasher);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_update(IntPtr hasher, byte[] input, ulong size);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_update(IntPtr hasher, ref byte input, ulong size);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_finalize(IntPtr hasher, byte[] output);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_finalize_xof(IntPtr hasher, byte[] output, ulong size);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this._hasher != IntPtr.Zero)
            {
                blake3_delete(this._hasher);
                this._hasher = IntPtr.Zero;
            }
        }

        public Blake3NetBlake3(int bitLength)
        {
            if (bitLength < 8 || bitLength % 8 != 0)
            {
                throw new ArgumentException($"Invalid {nameof(bitLength)}");
            }
            this.bitLength = bitLength;
            this.outputSize = bitLength / 8;
        }

        public override void Initialize()
        {
            this._hasher = blake3_new();
            if (this._hasher != IntPtr.Zero)
            {
                blake3_reset(this._hasher);
            }
            else
            {
                throw new NullReferenceException("Initialization failed");
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new Blake3NetBlake3(this.bitLength);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._hasher == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                blake3_update(this._hasher, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                blake3_update(this._hasher, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._hasher == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.outputSize];
            if (this.outputSize == defaultOutputSize)
            {
                blake3_finalize(this._hasher, resultBuffer);
            }
            else
            {
                blake3_finalize_xof(this._hasher, resultBuffer, (ulong)this.outputSize);
            }
            return resultBuffer;
        }
    }
}
