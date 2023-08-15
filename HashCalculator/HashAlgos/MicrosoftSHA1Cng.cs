using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MicrosoftSHA1Cng : MicrosoftHashDigest
    {
        public override string AlgoName => "SHA-1";

        public override AlgoType AlgoType => AlgoType.SHA1;

        public MicrosoftSHA1Cng() : base(new SHA1Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MicrosoftSHA1Cng();
        }
    }
}
