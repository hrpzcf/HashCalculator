using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BouncyCastleSha224 : BouncyCastleDigest
    {
        public override string AlgoName => "SHA-224";

        public override AlgoType AlgoGroup => AlgoType.SHA224;

        public BouncyCastleSha224() : base(new Sha224Digest(), 224)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BouncyCastleSha224();
        }
    }
}
