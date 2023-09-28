using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpBlake2b : BouncyCastleDigest
    {
        private readonly int bitLength;

        public override string AlgoName => $"BLAKE2b-{this.bitLength}";

        public override AlgoType AlgoGroup => AlgoType.BLAKE2B;

        public BCSharpBlake2b(int bitLength) : base(new Blake2bDigest(bitLength), bitLength)
        {
            this.bitLength = bitLength;
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpBlake2b(this.bitLength);
        }
    }
}
