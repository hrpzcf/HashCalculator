using System;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class HashingXxHash32 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly XxHash32 algo = new XxHash32();

        public string AlgoName => "XxHash32";

        public AlgoType AlgoType => AlgoType.XXHASH32;

        public override void Initialize()
        {
            this.algo.Reset();
        }

        public IHashAlgoInfo NewInstance()
        {
            return new HashingXxHash32();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (cbSize > 0)
            {
                this.algo.Append(new ReadOnlySpan<byte>(array, ibStart, cbSize));
            }
        }

        protected override byte[] HashFinal()
        {
            return this.algo.GetHashAndReset();
        }
    }
}
