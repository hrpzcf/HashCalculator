using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleBlake3 : HashAlgorithm
    {
        private readonly Blake3Digest blake3Digest;

        public BouncyCastleBlake3(int bitLength)
        {
            this.blake3Digest = new Blake3Digest(bitLength);
        }

        public override void Initialize()
        {
            this.blake3Digest.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.blake3Digest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.blake3Digest.GetDigestSize();
            byte[] computedBlake3Result = new byte[size];
            this.blake3Digest.DoFinal(computedBlake3Result, 0);
            return computedBlake3Result;
        }
    }
}
