using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpWhirlpool : BCSharpDigest
    {
        private const int bitLength = 512;

        public override string AlgoName => "Whirlpool";

        public override AlgoType AlgoGroup => AlgoType.WHIRLPOOL;

        public BCSharpWhirlpool() : base(new WhirlpoolDigest(), bitLength)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpWhirlpool();
        }
    }
}
