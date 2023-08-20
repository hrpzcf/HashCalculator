using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashCalculator
{
    internal class FileAlgosHashs
    {
        public bool FileIndependent { get; set; }

        public bool NameInBasisExist { get; set; }

        public FileAlgosHashs() { }

        public FileAlgosHashs(byte[] initHash)
        {
            this.AddAlgoHash(string.Empty, initHash);
        }

        public FileAlgosHashs(string algoName, byte[] initHash)
        {
            this.AddAlgoHash(algoName, initHash);
        }

        private Dictionary<string, List<byte[]>> AlgosHashs { get; } =
            new Dictionary<string, List<byte[]>>();

        public bool AddAlgoHash(string algoName, byte[] hashBytes)
        {
            if (algoName == null || hashBytes == null)
            {
                return false;
            }
            if (this.AlgosHashs.TryGetValue(algoName, out List<byte[]> hashValues))
            {
                hashValues.Add(hashBytes);
            }
            else
            {
                this.AlgosHashs.Add(algoName, new List<byte[]>()
                {
                    hashBytes
                });
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

        private bool AddHashAndName(string algoName, string hashString, string fileName)
        {
            if (algoName is null || hashString is null || fileName is null)
            {
                return false;
            }
            if (CommonUtils.HashFromAnyString(hashString) is byte[] hash)
            {
                // 虽然查询时会忽略文件名的大小写，但也要避免储存仅大小写不同的键
                // 因为查询时有可能每次都随机匹配到它们其中的一个(取决于 Keys 顺序是否固定)
                fileName = fileName.Trim(new char[] { '*', ' ', '\n' }).ToLower();
                if (this.FileHashDict.ContainsKey(fileName))
                {
                    this.FileHashDict[fileName].AddAlgoHash(algoName, hash);
                }
                else
                {
                    this.FileHashDict[fileName] = new FileAlgosHashs(algoName, hash);
                }
                if (fileName == string.Empty)
                {
                    this.FileHashDict[fileName].FileIndependent = true;
                }
                return true;
            }
            return false;
        }

        private bool AddOnlyHashString(string hashString)
        {
            return this.AddHashAndName(string.Empty, hashString, string.Empty);
        }

        public string UpdateWithFile(string filePath)
        {
            this.FileHashDict.Clear();
            this.ReasonForFailure = null;
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string line;
                        int readedLineCount = 0;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] items = line.Split(
                                new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            // 对应格式：hash-string *file-fileName
                            if (items.Length == 2)
                            {
                                if (this.AddHashAndName(string.Empty, items[0], items[1]))
                                {
                                    ++readedLineCount;
                                }
                            }
                            // 对应格式：#hash-fileName *hash-string *file-fileName
                            else if (items.Length == 3)
                            {
                                if (this.AddHashAndName(
                                    items[0].Trim(new char[] { '#', ' ' }),
                                    items[1].Trim(new char[] { ' ', '*' }), items[2]))
                                {
                                    ++readedLineCount;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (readedLineCount <= 0)
                        {
                            this.ReasonForFailure = "没有收集到任何校验依据，请检查校验依据文件内容";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ReasonForFailure = $"异常导致收集校验依据失败：\n{ex.Message}";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithHash(string hash)
        {
            this.FileHashDict.Clear();
            this.ReasonForFailure = this.AddOnlyHashString(hash.Trim()) ?
                null : "收集校验依据失败，可能是哈希值格式不正确或不存在该校验依据文件";
            return this.ReasonForFailure;
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
            foreach (string dictFileName in this.FileHashDict.Keys)
            {
                // Windows 文件名不区分大小写，查找时忽略大小写
                if (dictFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return this.FileHashDict[dictFileName];
                }
            }
            return default(FileAlgosHashs);
        }

        public CmpRes VerifyHash(string fileName, string algoName, byte[] hash)
        {
            if (fileName == null || algoName == null || hash == null
                || !this.FileHashDict.Any())
            {
                return CmpRes.Unrelated;
            }
            if (this.GetFileAlgosHashs(fileName) is FileAlgosHashs fileAlgosHashs)
            {
                return fileAlgosHashs.CompareHash(algoName, hash);
            }
            return CmpRes.Unrelated;
        }

        public string ReasonForFailure { get; private set; }

        public Dictionary<string, FileAlgosHashs> FileHashDict { get; } =
            new Dictionary<string, FileAlgosHashs>();
    }
}
