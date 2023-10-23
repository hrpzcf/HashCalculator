using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class Gost34112012Streebog : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private readonly int outputSize;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr _state = IntPtr.Zero;

        public string AlgoName => $"Streebog-{this.bitLength}";

        public AlgoType AlgoType
        {
            get
            {
                if (this.algoType == AlgoType.Unknown)
                {
                    if (Enum.TryParse($"STREEBOG_{this.bitLength}", true, out AlgoType algo))
                    {
                        this.algoType = algo;
                    }
                    else
                    {
                        this.algoType = AlgoType.STREEBOG;
                    }
                }
                return this.algoType;
            }
        }

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr streebog_new();

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern void streebog_delete(IntPtr state);

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern int streebog_init(IntPtr state, uint bitLength);

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern void streebog_update(IntPtr state, byte[] input, ulong size);

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern void streebog_update(IntPtr state, ref byte input, ulong size);

        [DllImport(DllName.Streebog, CallingConvention = CallingConvention.Cdecl)]
        private static extern void streebog_final(IntPtr state, byte[] output, ulong size);

        public Gost34112012Streebog(int bitLength)
        {
            if (bitLength < 8 || bitLength % 8 != 0)
            {
                throw new ArgumentException($"Invalid bit length");
            }
            this.bitLength = bitLength;
            this.outputSize = bitLength / 8;
        }

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                streebog_delete(this._state);
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
            this._state = streebog_new();
            if (this._state == IntPtr.Zero ||
                streebog_init(this._state, (uint)this.bitLength) > 0)
            {
                throw new Exception("Initialization failed");
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new Gost34112012Streebog(this.bitLength);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                streebog_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                streebog_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.outputSize];
            streebog_final(this._state, resultBuffer, (ulong)this.outputSize);
            return resultBuffer;
        }
    }
}
