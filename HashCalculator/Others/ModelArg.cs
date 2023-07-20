namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class ModelArg
    {
        public readonly string filepath;
        public readonly byte[] expected;
        public readonly bool deprecated;

        public AlgoType PresetAlgo { get; set; }

        /// <summary>
        /// 约定：hash 长度 0 表示无法确定是否匹配
        /// </summary>
        public ModelArg(byte[] hash, string path, AlgoType algo)
        {
            this.filepath = path;
            this.expected = hash;
            this.deprecated = false;
            this.PresetAlgo = algo;
        }

        public ModelArg(string path, AlgoType algo)
        {
            this.filepath = path;
            this.expected = null;
            this.deprecated = false;
            this.PresetAlgo = algo;
        }

        public ModelArg(string path, bool deprecated, AlgoType algo)
        {
            this.filepath = path;
            this.expected = null;
            this.deprecated = deprecated;
            this.PresetAlgo = algo;
        }
    }
}
