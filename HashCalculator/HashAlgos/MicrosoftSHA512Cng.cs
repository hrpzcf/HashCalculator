using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MicrosoftSHA512Cng : MicrosoftHashDigest
    {
        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA512;

        public MicrosoftSHA512Cng() : base(new SHA512Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MicrosoftSHA512Cng();
        }
    }
}
