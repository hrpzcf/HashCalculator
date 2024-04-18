using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA512 : NetCryptoCngAbs
    {
        public override int DigestLength => 64;

        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA_512;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA512();
        }

        public NetCryptoCngSHA512() : base(new SHA512Cng())
        {
        }
    }
}
