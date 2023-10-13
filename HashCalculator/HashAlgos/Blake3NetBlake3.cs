using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class Blake3NetBlake3 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr hasher = IntPtr.Zero;
        private const int defaultByteLength = 32;

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
        private static extern void blake3_update(IntPtr hasher, byte[] input, long size);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_finalize(IntPtr hasher, byte[] output);

        [DllImport(DllName.Blake3, CallingConvention = CallingConvention.Cdecl)]
        private static extern void blake3_finalize_xof(IntPtr hasher, byte[] output, int size);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.hasher != IntPtr.Zero)
            {
                blake3_delete(this.hasher);
                this.hasher = IntPtr.Zero;
            }
        }

        public Blake3NetBlake3(int bitLength)
        {
            if (bitLength < 8 || bitLength % 8 != 0)
            {
                throw new ArgumentException($"Invalid {nameof(bitLength)}");
            }
            this.bitLength = bitLength;
        }

        public override void Initialize()
        {
            this.hasher = blake3_new();
            if (this.hasher != IntPtr.Zero)
            {
                blake3_reset(this.hasher);
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new Blake3NetBlake3(this.bitLength);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this.hasher == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart != 0)
            {
                array = array.Skip(ibStart).ToArray();
            }
            blake3_update(this.hasher, array, cbSize);
        }

        protected override byte[] HashFinal()
        {
            if (this.hasher == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            int outLength = this.bitLength / 8;
            byte[] resultBytes = new byte[outLength];
            if (outLength == defaultByteLength)
            {
                blake3_finalize(this.hasher, resultBytes);
            }
            else
            {
                blake3_finalize_xof(this.hasher, resultBytes, outLength);
            }
            return resultBytes;
        }
    }
}
