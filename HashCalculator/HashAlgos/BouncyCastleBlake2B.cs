using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BouncyCastleBlake2b : BouncyCastleDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"BLAKE2b-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2B;

        public BouncyCastleBlake2b(int bitLength) : base(new Blake2bDigest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BouncyCastleBlake2b(this.bitLength);
        }
    }
}
