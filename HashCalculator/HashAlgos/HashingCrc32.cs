using System;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class HashingCrc32 : HashAlgorithm, IHashAlgoInfo
    {
        private readonly Crc32 algo = new Crc32();

        public string AlgoName => "Crc32";

        public AlgoType AlgoType => AlgoType.CRC32;

        public override void Initialize()
        {
            this.algo.Reset();
        }

        public IHashAlgoInfo NewInstance()
        {
            return new HashingCrc32();
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
            byte[] hashBytes = this.algo.GetHashAndReset();
            Array.Reverse(hashBytes);
            return hashBytes;
        }
    }
}
