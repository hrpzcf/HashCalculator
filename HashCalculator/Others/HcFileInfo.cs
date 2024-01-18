namespace HashCalculator
{
    internal class HcFileInfo
    {
        public HcFileInfo(AlgoType algoType, byte[] bytes, long start, long count)
        {
            this.AlgoType = algoType;
            this.HashValueBytes = bytes;
            this.RealStart = start;
            this.RealCount = count;
        }

        public AlgoType AlgoType { get; }

        public byte[] HashValueBytes { get; }

        public long RealStart { get; }

        public long RealCount { get; }
    }
}
