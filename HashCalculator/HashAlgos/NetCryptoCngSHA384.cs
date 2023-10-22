using System.Security.Cryptography;

namespace HashCalculator
{
    internal class NetCryptoCngSHA384 : NetCryptoCngAbs
    {
        public override string AlgoName => "SHA-384";

        public override AlgoType AlgoType => AlgoType.SHA384;

        public NetCryptoCngSHA384() : base(new SHA384Cng())
        {
        }

        public override IHashAlgoInfo NewInstance()
        {
            return new NetCryptoCngSHA384();
        }
    }
}
