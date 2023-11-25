using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA1 : NetCryptoCngAbs
    {
        public override int DigestLength => 20;

        public override string AlgoName => "SHA-1";

        public override AlgoType AlgoType => AlgoType.SHA1;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA1();
        }

        public NetCryptoCngSHA1() : base(new SHA1Cng())
        {
        }
    }
}
