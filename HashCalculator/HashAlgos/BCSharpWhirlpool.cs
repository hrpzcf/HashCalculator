using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpWhirlpool : BouncyCastleDigest
    {
        public override string AlgoName => "Whirlpool";

        public override AlgoType AlgoGroup => AlgoType.WHIRLPOOL;

        public BCSharpWhirlpool() : base(new WhirlpoolDigest(), 512)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpWhirlpool();
        }
    }
}
