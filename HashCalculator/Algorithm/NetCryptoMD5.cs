using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoMD5 : NetCryptoAbstract
    {
        public override int DigestLength => 16;

        public override string AlgoName => "MD5";

        public override AlgoType AlgoType => AlgoType.MD5;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoMD5();
        }

        public NetCryptoMD5() : base(MD5.Create())
        {
        }
    }
}
