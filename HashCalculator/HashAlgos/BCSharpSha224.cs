using Org.BouncyCastle.Crypto.Digests;

namespace HashCalculator
{
    internal class BCSharpSha224 : BCSharpDigest
    {
        public override string AlgoName => "SHA-224";

        public override AlgoType AlgoGroup => AlgoType.SHA224;

        public BCSharpSha224() : base(new Sha224Digest(), 224)
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new BCSharpSha224();
        }
    }
}
