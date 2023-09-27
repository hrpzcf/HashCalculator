using Force.Crc32;

namespace HashCalculator
{
    internal class ForceCrc32NetCrc32C : Crc32CAlgorithm, IHashAlgoInfo
    {
        public string AlgoName => "Crc32C";

        public AlgoType AlgoType => AlgoType.CRC32C;

        public IHashAlgoInfo NewInstance()
        {
            return new ForceCrc32NetCrc32C();
        }
    }
}
