using System.Collections.Generic;

namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class HashModelArg
    {
        public string FilePath { get; }

        public bool InvalidFileName { get; }

        public bool Deprecated { get; }

        public IEnumerable<AlgoType> PresetAlgos { get; set; }

        public HashChecklist HashChecklist { get; set; }

        public HashModelArg(string path, IEnumerable<AlgoType> algo)
        {
            this.FilePath = path;
            this.PresetAlgos = algo;
        }

        public HashModelArg(string path, bool deprecated, IEnumerable<AlgoType> algo)
        {
            this.FilePath = path;
            this.Deprecated = deprecated;
            this.PresetAlgos = algo;
        }

        public HashModelArg(bool deprecated, bool invalidFname, IEnumerable<AlgoType> algo)
        {
            this.Deprecated = deprecated;
            this.InvalidFileName = invalidFname;
            this.PresetAlgos = algo;
        }

        public HashModelArg(HashChecklist checklist, string path, IEnumerable<AlgoType> algo)
        {
            this.FilePath = path;
            this.HashChecklist = checklist;
            this.PresetAlgos = algo;
        }
    }
}
