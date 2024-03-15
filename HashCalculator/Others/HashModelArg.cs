using System.Collections.Generic;
using System.IO;

namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class HashModelArg
    {
        public string RootDir { get; }

        public string FileName { get; }

        public string FilePath { get; }

        public string FileRelativePath { get; }

        public string Message { get; set; }

        public bool Deprecated { get; set; }

        public bool IsInvalidName { get; set; }

        public IEnumerable<AlgoType> PresetAlgos { get; set; }

        public HashChecklist HashChecklist { get; }

        public HashModelArg(IEnumerable<AlgoType> algo)
        {
            this.RootDir = null;
            this.FileName = "无效的文件名";
            this.FilePath = this.FileName;
            this.FileRelativePath = string.Empty;
            this.Message = "文件名含有禁止使用的字符";
            this.Deprecated = true;
            this.IsInvalidName = true;
            this.PresetAlgos = algo;
            this.HashChecklist = null;
        }

        public HashModelArg(string path, IEnumerable<AlgoType> algo)
        {
            this.RootDir = null;
            this.FileName = path;
            this.FilePath = path;
            this.FileRelativePath = path;
            this.Message = "找不到文件，可能搜索策略设置不正确";
            this.Deprecated = true;
            this.IsInvalidName = false;
            this.PresetAlgos = algo;
            this.HashChecklist = null;
        }

        public HashModelArg(string root, string path, IEnumerable<AlgoType> algo, HashChecklist checklist)
        {
            this.RootDir = root;
            this.FileName = Path.GetFileName(path);
            this.FilePath = path;
            this.Deprecated = false;
            this.IsInvalidName = false;
            this.PresetAlgos = algo;
            this.HashChecklist = checklist;
            if (!string.IsNullOrWhiteSpace(root) &&
                CommonUtils.GetRelativePath(root, path) is string relativePath)
            {
                this.FileRelativePath = relativePath;
            }
            else
            {
                this.FileRelativePath = this.FileName;
            }
        }
    }
}
