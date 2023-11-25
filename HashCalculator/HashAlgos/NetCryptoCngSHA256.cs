using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA256 : NetCryptoCngAbs
    {
        public override int DigestLength => 32;

        public override string AlgoName => "SHA-256";

        public override AlgoType AlgoType => AlgoType.SHA256;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA256();
        }

        public NetCryptoCngSHA256() : base(new SHA256Cng())
        {
        }
    }
}
