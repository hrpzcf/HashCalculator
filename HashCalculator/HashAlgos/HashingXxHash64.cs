using System;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class HashingXxHash64 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly XxHash64 algo = new XxHash64();

        public string AlgoName => "XxHash64";

        public AlgoType AlgoType => AlgoType.XxHash64;

        public override void Initialize()
        {
            this.algo.Reset();
        }

        public IHashAlgoInfo NewInstance()
        {
            return new HashingXxHash64();
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
            return this.algo.GetCurrentHash();
        }
    }
}
