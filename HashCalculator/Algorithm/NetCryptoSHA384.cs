using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoSHA384 : NetCryptoAbstract
    {
        public override int DigestLength => 48;

        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA_384;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoSHA384();
        }

        public NetCryptoSHA384() : base(SHA384.Create())
        {
        }
    }
}
