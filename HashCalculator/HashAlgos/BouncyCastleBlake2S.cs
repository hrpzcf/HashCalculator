using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BouncyCastleBlake2s : BouncyCastleDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"BLAKE2s-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2S;

        public BouncyCastleBlake2s(int bitLength) : base(new Blake2sDigest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BouncyCastleBlake2s(this.bitLength);
        }
    }
}
