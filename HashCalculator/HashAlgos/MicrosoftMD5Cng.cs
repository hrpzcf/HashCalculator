using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MicrosoftMD5Cng : MicrosoftHashDigest
    {
        public override string AlgoName => "MD5";

        public override AlgoType AlgoType => AlgoType.MD5;

        public MicrosoftMD5Cng() : base(new MD5Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MicrosoftMD5Cng();
        }
    }
}
