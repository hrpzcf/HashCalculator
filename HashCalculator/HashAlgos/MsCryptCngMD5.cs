using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptCngMD5 : MicrosoftHashDigest
    {
        public override string AlgoName => "MD5";

        public override AlgoType AlgoType => AlgoType.MD5;

        public MsCryptCngMD5() : base(new MD5Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptCngMD5();
        }
    }
}
