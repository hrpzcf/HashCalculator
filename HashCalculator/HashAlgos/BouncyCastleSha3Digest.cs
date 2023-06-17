using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleSha3Digest : HashAlgorithm
    {
        private readonly Sha3Digest sha3Digest;

        public BouncyCastleSha3Digest(int bitLength)
        {
            this.sha3Digest = new Sha3Digest(bitLength);
        }

        public override void Initialize()
        {
            this.sha3Digest?.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.sha3Digest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.sha3Digest.GetDigestSize();
            byte[] computedSha3Result = new byte[size];
            this.sha3Digest.DoFinal(computedSha3Result, 0);
            return computedSha3Result;
        }
    }
}
