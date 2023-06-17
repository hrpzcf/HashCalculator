using Org.BouncyCastle.Crypto;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class BouncyCastleIDigest<TDigest> :
        HashAlgorithm where TDigest : IDigest, new()
    {
        private readonly TDigest iDigestObject;

        public BouncyCastleIDigest()
        {
            this.iDigestObject = new TDigest();
        }

        public override void Initialize()
        {
            this.iDigestObject.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.iDigestObject.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.iDigestObject.GetDigestSize();
            byte[] computedDigestByteArray = new byte[size];
            this.iDigestObject.DoFinal(computedDigestByteArray, 0);
            return computedDigestByteArray;
        }
    }
}
