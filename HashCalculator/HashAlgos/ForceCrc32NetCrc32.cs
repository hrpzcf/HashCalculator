using Force.Crc32;

namespace HashCalculator
{
    internal class ForceCrc32NetCrc32 : Crc32Algorithm, IHashAlgoInfo
    {
        public string AlgoName => "Crc32";

        public AlgoType AlgoType => AlgoType.CRC32;

        public IHashAlgoInfo NewInstance()
        {
            return new ForceCrc32NetCrc32();
        }
    }
}
