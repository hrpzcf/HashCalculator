using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoSHA256 : NetCryptoAbs
    {
        public override int DigestLength => 32;

        public override string AlgoName => "SHA-256";

        public override AlgoType AlgoType => AlgoType.SHA_256;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoSHA256();
        }

        public NetCryptoSHA256() : base(SHA256.Create())
        {
        }
    }
}
