using Org.BouncyCastle.Crypto.Digests;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleSha224Digest : HashAlgorithm
    {
        private readonly Sha224Digest sha224digest;

        public BouncyCastleSha224Digest()
        {
            this.sha224digest = new Sha224Digest();
        }

        public override void Initialize()
        {
            this.sha224digest?.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.sha224digest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.sha224digest.GetDigestSize();
            byte[] sha224ComputeResult = new byte[size];
            this.sha224digest.DoFinal(sha224ComputeResult, 0);
            return sha224ComputeResult;
        }
    }
}
