using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoSHA512 : NetCryptoAbs
    {
        public override int DigestLength => 64;

        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA_512;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoSHA512();
        }

        public NetCryptoSHA512() : base(SHA512.Create())
        {
        }
    }
}
