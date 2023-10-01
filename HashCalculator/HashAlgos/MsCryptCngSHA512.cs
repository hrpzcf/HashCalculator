using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptCngSHA512 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA512;

        public MsCryptCngSHA512() : base(new SHA512Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptCngSHA512();
        }
    }
}
