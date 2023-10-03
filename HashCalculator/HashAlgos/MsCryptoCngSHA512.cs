using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptoCngSHA512 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA512;

        public MsCryptoCngSHA512() : base(new SHA512Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptoCngSHA512();
        }
    }
}
