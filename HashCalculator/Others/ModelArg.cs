namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class ModelArg
    {
        public string FilePath { get; }

        public bool InvalidFileName { get; }

        public bool Deprecated { get; }

        public AlgoType PresetAlgo { get; set; }

        public HashChecklist HashChecklist { get; set; }

        public ModelArg(string path, AlgoType algo)
        {
            this.FilePath = path;
            this.PresetAlgo = algo;
        }

        public ModelArg(HashChecklist checklist, string path, AlgoType algo)
        {
            this.FilePath = path;
            this.HashChecklist = checklist;
            this.PresetAlgo = algo;
        }

        public ModelArg(string path, bool deprecated, AlgoType algo)
        {
            this.FilePath = path;
            this.Deprecated = deprecated;
            this.PresetAlgo = algo;
        }

        public ModelArg(bool deprecated, bool invalidFname, AlgoType algo)
        {
            this.Deprecated = deprecated;
            this.InvalidFileName = invalidFname;
            this.PresetAlgo = algo;
        }
    }
}
