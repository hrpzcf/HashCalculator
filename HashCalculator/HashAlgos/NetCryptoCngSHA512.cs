using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA512 : NetCryptoCngAbs
    {
        public override string AlgoName => "SHA-512";

        public override AlgoType AlgoType => AlgoType.SHA512;

        public NetCryptoCngSHA512() : base(new SHA512Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA512();
        }
    }
}
