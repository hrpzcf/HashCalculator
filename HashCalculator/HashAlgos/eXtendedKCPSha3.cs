using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class ExtendedKCPSha3 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private readonly int outputSize;
        private int _errorCode = 0;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr _state = IntPtr.Zero;

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr keccak_new();

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern void keccak_delete(IntPtr state);

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_init(IntPtr state, int bitLength);

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_update(IntPtr state, byte[] input, ulong size);

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_update(IntPtr state, ref byte input, ulong size);

        [DllImport(DllName.Keccak, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_final(IntPtr state, byte[] output, ulong size);

        public string AlgoName => $"SHA3-{this.bitLength}";

        public AlgoType AlgoType
        {
            get
            {
                if (this.algoType == AlgoType.Unknown)
                {
                    if (Enum.TryParse($"SHA3_{this.bitLength}", true, out AlgoType algo))
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

        public ExtendedKCPSha3(int bitLength)
        {
            switch (bitLength)
            {
                case 224:
                case 256:
                case 384:
                case 512:
                    break;
                default:
                    throw new ArgumentException($"Invalid bit length");
            }
            this.bitLength = bitLength;
            this.outputSize = bitLength / 8;
        }

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                keccak_delete(this._state);
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
            this._state = keccak_new();
            if (this._state == IntPtr.Zero || sha3_init(this._state, this.bitLength) > 0)
            {
                throw new NullReferenceException("Initialization failed");
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new ExtendedKCPSha3(this.bitLength);
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
                this._errorCode = sha3_update(this._state, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = sha3_update(this._state, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            byte[] resultBuffer = new byte[this.outputSize];
            if (sha3_final(this._state, resultBuffer, (ulong)this.outputSize) > 0)
            {
                throw new Exception("Finalize hash fialed");
            }
            return resultBuffer;
        }
    }
}
