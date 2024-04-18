using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngMD5 : NetCryptoCngAbs
    {
        public override int DigestLength => 16;

        public override string AlgoName => "MD5";

        public override AlgoType AlgoType => AlgoType.MD5;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngMD5();
        }

        public NetCryptoCngMD5() : base(new MD5Cng())
        {
        }
    }
}
