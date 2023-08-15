using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MicrosoftSHA384Cng : MicrosoftHashDigest
    {
        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA384;

        public MicrosoftSHA384Cng() : base(new SHA384Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MicrosoftSHA384Cng();
        }
    }
}
