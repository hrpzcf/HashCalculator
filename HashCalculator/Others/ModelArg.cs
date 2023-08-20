namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class ModelArg
    {
        public string FilePath { get; }

        public bool Deprecated { get; }

        public AlgoType PresetAlgo { get; set; }

        public HashBasis HashBasis { get; set; }

        public ModelArg(string path, AlgoType algo)
        {
            this.FilePath = path;
            this.PresetAlgo = algo;
        }

        public ModelArg(HashBasis basis, string path, AlgoType algo)
        {
            this.FilePath = path;
            this.HashBasis = basis;
            this.PresetAlgo = algo;
        }

        public ModelArg(string path, bool deprecated, AlgoType algo)
        {
            this.FilePath = path;
            this.Deprecated = deprecated;
            this.PresetAlgo = algo;
        }
    }
}
