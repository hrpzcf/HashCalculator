using Org.BouncyCastle.Crypto;
using System;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal abstract class BouncyCastleDigest : HashAlgorithm, IHashAlgoInfo
    {
        private readonly int bitLength;
        private readonly IDigest bouncyCastleDigest;
        private AlgoType algoType = AlgoType.Unknown;

        public BouncyCastleDigest(IDigest digest, int bitLength)
        {
            this.bitLength = bitLength;
            this.bouncyCastleDigest = digest;
        }

        public abstract string AlgoName { get; }

        public abstract AlgoType AlgoGroup { get; }

        public AlgoType AlgoType
        {
            get
            {
                if (this.algoType == AlgoType.Unknown)
                {
                    if (Enum.TryParse($"{this.AlgoGroup}_{this.bitLength}",
                        true, out AlgoType algo))
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

        public abstract IHashAlgoInfo NewInstance();

        public override void Initialize()
        {
            this.bouncyCastleDigest.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.bouncyCastleDigest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.bouncyCastleDigest.GetDigestSize();
            byte[] bouncyCastleHashResult = new byte[size];
            this.bouncyCastleDigest.DoFinal(bouncyCastleHashResult, 0);
            return bouncyCastleHashResult;
        }
    }
}
