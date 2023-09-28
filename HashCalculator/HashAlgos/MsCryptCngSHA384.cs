using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptCngSHA384 : MicrosoftHashDigest
    {
        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA384;

        public MsCryptCngSHA384() : base(new SHA384Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptCngSHA384();
        }
    }
}
