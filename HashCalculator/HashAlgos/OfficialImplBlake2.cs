using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal abstract class OfficialImplBlake2 : HashAlgorithm, IHashAlgoInfo
    {
        public readonly int bitLength;
        private AlgoType algoType = AlgoType.Unknown;
        private int _errorCode = 0;
        private IntPtr _statePtr = IntPtr.Zero;

        public abstract string NamePrefix { get; }

        public abstract AlgoType AlgoGroup { get; }

        public abstract int MaxOutputSize { get; }

        public int DigestLength { get; }

        public string AlgoName => $"{this.NamePrefix}-{this.bitLength}";

        public AlgoType AlgoType
        {
            get
            {
                if (this.algoType == AlgoType.Unknown)
                {
                    if (Enum.TryParse($"{this.AlgoGroup}_{this.bitLength}", true, out AlgoType algo))
                    {
                        this.algoType = algo;
                    }
                    else
                    {
                        this.algoType = this.AlgoGroup;
                    }
                }
                return this.algoType;
            }
        }

        public abstract void Blake2DeleteState(IntPtr statePtr);

        public abstract IntPtr Blake2New();

        public abstract int Blake2Init(IntPtr statePtr, ulong outlen);

        public abstract int Blake2Update(IntPtr statePtr, byte[] input, ulong inlen);

        public abstract int Blake2Update(IntPtr statePtr, ref byte input, ulong inlen);

        public abstract int Blake2Final(IntPtr statePtr, byte[] output, ulong outlen);

        public abstract IHashAlgoInfo NewInstance();

        private void DeleteState()
        {
            if (this._statePtr != IntPtr.Zero)
            {
                this.Blake2DeleteState(this._statePtr);
                this._statePtr = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.DeleteState();
            base.Dispose(disposing);
        }

        public OfficialImplBlake2(int bitLength)
        {
            int bytesNumber = bitLength / 8;
            if (bitLength < 8 || bitLength % 8 != 0 || bytesNumber > this.MaxOutputSize)
            {
                throw new ArgumentException($"Invalid bit length");
            }
            this.bitLength = bitLength;
            this.DigestLength = bytesNumber;
        }

        public override void Initialize()
        {
            this._errorCode = 0;
            this.DeleteState();
            this._statePtr = this.Blake2New();
            if (this._statePtr != IntPtr.Zero)
            {
                this._errorCode = this.Blake2Init(this._statePtr, (ulong)this.DigestLength);
            }
            else
            {
                throw new Exception("Initialization failed");
            }
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode != 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            if (ibStart != 0 || cbSize != array.Length)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this._errorCode = this.Blake2Update(this._statePtr, ref input, (ulong)cbSize);
            }
            else
            {
                this._errorCode = this.Blake2Update(this._statePtr, array, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            if (this._statePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not initialized yet");
            }
            else if (this._errorCode != 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            byte[] resultBuffer = new byte[this.DigestLength];
            if (this.Blake2Final(this._statePtr, resultBuffer, (ulong)this.DigestLength) != 0)
            {
                throw new InvalidOperationException("An error has occurred");
            }
            return resultBuffer;
        }
    }
}
