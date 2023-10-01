using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpBlake3 : BCSharpDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"BLAKE3-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.BLAKE3;

        public BCSharpBlake3(int bitLength) : base(new Blake3Digest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpBlake3(this.bitLength);
        }
    }
}
