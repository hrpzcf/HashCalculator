using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoSHA1 : NetCryptoAbs
    {
        public override int DigestLength => 20;

        public override string AlgoName => "SHA-1";

        public override AlgoType AlgoType => AlgoType.SHA_1;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoSHA1();
        }

        public NetCryptoSHA1() : base(SHA1.Create())
        {
        }
    }
}
