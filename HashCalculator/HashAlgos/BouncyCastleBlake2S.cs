using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleBlake2S : HashAlgorithm
    {
        private readonly Blake2sDigest blake2sDigest;

        public BouncyCastleBlake2S(int bitLength)
        {
            this.blake2sDigest = new Blake2sDigest(bitLength);
        }

        public override void Initialize()
        {
            this.blake2sDigest.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.blake2sDigest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.blake2sDigest.GetDigestSize();
            byte[] computedBlake3Result = new byte[size];
            this.blake2sDigest.DoFinal(computedBlake3Result, 0);
            return computedBlake3Result;
        }
    }
}
