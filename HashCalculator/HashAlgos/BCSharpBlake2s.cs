using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpBlake2s : BouncyCastleDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"BLAKE2s-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2S;

        public BCSharpBlake2s(int bitLength) : base(new Blake2sDigest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpBlake2s(this.bitLength);
        }
    }
}
