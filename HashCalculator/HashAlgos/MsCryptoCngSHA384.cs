using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptoCngSHA384 : MsCryptCngDigest
    {
        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA384;

        public MsCryptoCngSHA384() : base(new SHA384Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptoCngSHA384();
        }
    }
}
