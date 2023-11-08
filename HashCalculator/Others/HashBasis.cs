using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashCalculator
{
    internal class FileAlgosHashs
    {
        private bool FileIndependent { get; set; }

        private Dictionary<string, List<byte[]>> AlgosHashs { get; } =
            new Dictionary<string, List<byte[]>>();

        public bool NameInBasisExist { get; set; }

        public string OriginFileName { get; private set; }

        public FileAlgosHashs(string fileName, string algoName, byte[] hashBytes)
        {
            this.AddFileAlgoHash(fileName, algoName, hashBytes);
        }

        public bool AddFileAlgoHash(string fileName, string algoName, byte[] hashBytes)
        {
            this.OriginFileName = fileName;
            this.FileIndependent = fileName == string.Empty;
            if (algoName == null || hashBytes == null)
            {
                return false;
            }
            if (this.AlgosHashs.TryGetValue(algoName, out var hashValues))
            {
                hashValues.Add(hashBytes);
            }
            else
            {
                this.AlgosHashs[algoName] = new List<byte[]>() { hashBytes };
            }
            return true;
        }

        public CmpRes CompareHash(string algoName, byte[] hashBytes)
        {
            if (algoName == null || hashBytes == null || !this.AlgosHashs.Any())
            {
                return CmpRes.Unrelated;
            }
            if (!this.AlgosHashs.TryGetValue(algoName, out List<byte[]> hashValues))
            {
                this.AlgosHashs.TryGetValue(string.Empty, out hashValues);
            }
            if (hashValues != null)
            {
                if (!hashValues.Any())
                {
                    return CmpRes.Uncertain;
                }
                byte[] first = hashValues[0];
                if (hashValues.Count > 1)
                {
                    if (!hashValues.Skip(1).All(i => i.SequenceEqual(first)))
                    {
                        return CmpRes.Uncertain;
                    }
                }
                return first.SequenceEqual(hashBytes) ?
                    CmpRes.Matched : this.FileIndependent ? CmpRes.Unrelated : CmpRes.Mismatch;
            }
            return CmpRes.Unrelated;
        }

        public string[] GetExistsAlgoNames()
        {
            return this.AlgosHashs.Keys.Where(i => i != string.Empty).ToArray();
        }
    }

    internal class HashBasis
    {
        private static readonly char[] charsToTrim = { ' ', '*', '#', '\r', '\n' };

        public string ReasonForFailure { get; private set; }

        public Dictionary<string, FileAlgosHashs> FileHashDict { get; } =
            new Dictionary<string, FileAlgosHashs>();

        public HashBasis(string filePath)
        {
            this.UpdateWithFile(filePath);
        }

        public HashBasis() { }

        public bool IsNameInBasis(string fileName)
        {
            foreach (string nameInBasis in this.FileHashDict.Keys)
            {
                if (nameInBasis.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    this.FileHashDict[nameInBasis].NameInBasisExist = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 向校验依据对象添加一条以 <算法名>、<哈希字符串>、<文件名> 组成的依据
        /// </summary>
        private bool AddBasisItem(string algoName, string hashString, string fileName)
        {
            if (!(algoName is null || hashString is null || fileName is null))
            {
                hashString = hashString.Trim(charsToTrim);
                if (CommonUtils.HashFromAnyString(hashString) is byte[] hash)
                {
                    // 虽然查询时会忽略文件名的大小写，但也要避免储存仅大小写不同的键
                    // 因为查询时有可能每次都随机匹配到它们其中的一个(取决于 Keys 顺序是否固定)
                    algoName = algoName.Trim(charsToTrim);
                    fileName = fileName.Trim(charsToTrim);
                    string fnameForKey = fileName.ToLower();
                    if (this.FileHashDict.TryGetValue(fnameForKey, out FileAlgosHashs algosHashs1))
                    {
                        algosHashs1.AddFileAlgoHash(fileName, algoName, hash);
                    }
                    else
                    {
                        this.FileHashDict[fnameForKey] = new FileAlgosHashs(fileName, algoName, hash);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool AddBasisItemWithLine(string basisLine)
        {
            string[] items = basisLine.Split(
                new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            // 对应格式：hash-string
            if (items.Length == 1)
            {
                return this.AddBasisItem(string.Empty, items[0], string.Empty);
            }
            // 对应格式：hash-string *file-fileName
            else if (items.Length == 2)
            {
                return this.AddBasisItem(string.Empty, items[0], items[1]);
            }
            // 对应格式：#hash-name *hash-string *file-name
            else if (items.Length == 3 && this.AddBasisItem(items[0], items[1], items[2]))
            {
                return true;
            }
            return false;
        }

        public string UpdateWithFile(string filePath)
        {
            this.FileHashDict.Clear();
            this.ReasonForFailure = null;
            try
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string basisTextLine;
                        int readedLineCount = 0;
                        while ((basisTextLine = reader.ReadLine()) != null)
                        {
                            if (this.AddBasisItemWithLine(basisTextLine))
                            {
                                ++readedLineCount;
                            }
                        }
                        if (readedLineCount == 0)
                        {
                            this.ReasonForFailure = "没有收集到任何校验依据，请检查校验依据文件内容";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ReasonForFailure = $"出现异常导致收集校验依据失败：\n{ex.Message}";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithLines(string hashParagraph)
        {
            this.FileHashDict.Clear();
            int readedLineCount = 0;
            this.ReasonForFailure = null;
            foreach (string line in hashParagraph.Split('\r', '\n'))
            {
                if (this.AddBasisItemWithLine(line))
                {
                    ++readedLineCount;
                }
            }
            if (readedLineCount == 0)
            {
                this.ReasonForFailure = "没有收集到任何校验依据，请检查校验依据文件内容";
            }
            return this.ReasonForFailure;
        }

        public CmpRes VerifyHash(string fileName, string algoName, byte[] hash)
        {
            if (fileName == null || algoName == null || hash == null || !this.FileHashDict.Any())
            {
                return CmpRes.Unrelated;
            }
            if (this.GetFileAlgosHashs(fileName) is FileAlgosHashs fileAlgosHashs)
            {
                return fileAlgosHashs.CompareHash(algoName, hash);
            }
            return CmpRes.Unrelated;
        }

        public FileAlgosHashs GetFileAlgosHashs(string fileName)
        {
            if (fileName == null || !this.FileHashDict.Any())
            {
                return default(FileAlgosHashs);
            }
            if (this.FileHashDict.Count == 1 &&
                this.FileHashDict.TryGetValue(string.Empty, out var algosHashs))
            {
                return algosHashs;
            }
            foreach (string fileNameInDict in this.FileHashDict.Keys)
            {
                // Windows 文件名不区分大小写，查找时忽略大小写
                if (fileNameInDict.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return this.FileHashDict[fileNameInDict];
                }
            }
            return default(FileAlgosHashs);
        }
    }
}
