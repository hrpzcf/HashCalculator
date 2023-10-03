using System.Security.Cryptography;

namespace HashCalculator
{
    internal class MsCryptoCngMD5 : MsCryptCngDigest
    {
        public override string AlgoName => "MD5";

        public override AlgoType AlgoType => AlgoType.MD5;

        public MsCryptoCngMD5() : base(new MD5Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new MsCryptoCngMD5();
        }
    }
}
