using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BouncyCastleWhirlpool : BouncyCastleDigest
    {
        public override string AlgoName => "Whirlpool-512";

        public override AlgoType AlgoGroup => AlgoType.WHIRLPOOL;

        public BouncyCastleWhirlpool() : base(new WhirlpoolDigest(), 512)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BouncyCastleWhirlpool();
        }
    }
}
