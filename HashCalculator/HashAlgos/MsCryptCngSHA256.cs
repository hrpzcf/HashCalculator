using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptCngSHA256 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-256";

        public override AlgoType AlgoType => AlgoType.SHA256;

        public MsCryptCngSHA256() : base(new SHA256Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptCngSHA256();
        }
    }
}
