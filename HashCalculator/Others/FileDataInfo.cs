namespace HashCalculator
{
    internal class FileDataInfo
    {
        public FileDataInfo(long start, long count, bool tailable)
        {
            this.ActualStart = start;
            this.ActualCount = count;
            this.IsTailable = tailable;
            this.IsTagged = false;
        }

        public FileDataInfo(long start, long count, string algo, byte[] bytes, bool tailable)
        {
            this.ActualStart = start;
            this.ActualCount = count;
            this.AlgoName = algo;
            this.HashBytes = bytes;
            this.IsTailable = tailable;
            this.IsTagged = true;
        }

        /// <summary>
        /// 指示文件是否有本程序的 HCM 标记
        /// </summary>
        public bool IsTagged { get; }

        /// <summary>
        /// 指示文件是否可以在其结束标记后写入内容<br/>
        /// 目前只检测是否是 PNG/JPEG 文件及是否发现其结束标记来确定此属性是否为 true
        /// </summary>
        public bool IsTailable { get; }

        public string AlgoName { get; }

        public byte[] HashBytes { get; }

        public long ActualStart { get; }

        public long ActualCount { get; }
    }
}
