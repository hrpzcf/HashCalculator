using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MicrosoftSHA256Cng : MicrosoftHashDigest
    {
        public override string AlgoName => "SHA-256";

        public override AlgoType AlgoType => AlgoType.SHA256;

        public MicrosoftSHA256Cng() : base(new SHA256Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MicrosoftSHA256Cng();
        }
    }
}
