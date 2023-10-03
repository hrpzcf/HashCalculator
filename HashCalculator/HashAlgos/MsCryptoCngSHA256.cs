using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptoCngSHA256 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-256";

        public override AlgoType AlgoType => AlgoType.SHA256;

        public MsCryptoCngSHA256() : base(new SHA256Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptoCngSHA256();
        }
    }
}
