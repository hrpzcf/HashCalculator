using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA1 : NetCryptoCngAbs
    {
        public override string AlgoName => "SHA-1";

        public override AlgoType AlgoType => AlgoType.SHA1;

        public NetCryptoCngSHA1() : base(new SHA1Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA1();
        }
    }
}
