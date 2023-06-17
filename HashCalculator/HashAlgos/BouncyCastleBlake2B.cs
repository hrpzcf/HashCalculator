using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleBlake2B : HashAlgorithm
    {
        private readonly Blake2bDigest blake2bDigest;

        public BouncyCastleBlake2B(int bitLength)
        {
            this.blake2bDigest = new Blake2bDigest(bitLength);
        }

        public override void Initialize()
        {
            this.blake2bDigest.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.blake2bDigest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.blake2bDigest.GetDigestSize();
            byte[] computedBlake3Result = new byte[size];
            this.blake2bDigest.DoFinal(computedBlake3Result, 0);
            return computedBlake3Result;
        }
    }
}
