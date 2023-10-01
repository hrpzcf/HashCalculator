using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpSha3 : BCSharpDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"SHA3-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.SHA3;

        public BCSharpSha3(int bitLength) : base(new Sha3Digest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpSha3(this.bitLength);
        }
    }
}
