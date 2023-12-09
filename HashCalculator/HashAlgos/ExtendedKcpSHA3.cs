using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class ExtendedKcpSHA3 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private int _errorCode = 0;
        private AlgoType algoType = AlgoType.Unknown;
        private IntPtr _state = IntPtr.Zero;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sha3_new();

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sha3_delete(IntPtr state);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_init(IntPtr state, int bitLength);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_update(IntPtr state, byte[] input, ulong bitLength);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_update(IntPtr state, ref byte input, ulong bitLength);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sha3_final(IntPtr state, byte[] output);

        public int DigestLength { get; }

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

        public ExtendedKcpSHA3(int bitLength)
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
            this.DigestLength = bitLength / 8;
        }

        private void DeleteState()
        {
            if (this._state != IntPtr.Zero)
            {
                sha3_delete(this._state);
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
            this._state = sha3_new();
            if (this._state == IntPtr.Zero || sha3_init(this._state, this.bitLength) > 0)
            {
                throw new Exception("Initialization failed");
            }
        }

        public IHashAlgoInfo NewInstance()
        {
            return new ExtendedKcpSHA3(this.bitLength);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode > 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart == 0 && cbSize == array.Length)
            {
                this._errorCode = sha3_update(this._state, array, (ulong)cbSize << 3);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = sha3_update(this._state, ref input, (ulong)cbSize << 3);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._state == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode > 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            if (sha3_final(this._state, resultBuffer) > 0)
            {
                throw new Exception("Finalize hash failed");
            }
            return resultBuffer;
        }
    }
}
