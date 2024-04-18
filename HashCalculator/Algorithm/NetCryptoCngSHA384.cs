using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA384 : NetCryptoCngAbs
    {
        public override int DigestLength => 48;

        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA_384;

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA384();
        }

        public NetCryptoCngSHA384() : base(new SHA384Cng())
        {
        }
    }
}
