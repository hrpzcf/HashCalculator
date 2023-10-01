using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptCngSHA1 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-1";

        public override AlgoType AlgoType => AlgoType.SHA1;

        public MsCryptCngSHA1() : base(new SHA1Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptCngSHA1();
        }
    }
}
