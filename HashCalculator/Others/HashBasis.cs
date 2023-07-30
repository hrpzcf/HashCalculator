using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashCalculator
{
    internal class HashBasisDictValue
    {
        public HashBasisDictValue() { }

        public HashBasisDictValue(byte[] initHash)
        {
            this.HashList.Add(initHash);
        }

        public bool HasBeenChecked { get; set; }

        public List<byte[]> HashList { get; } = new List<byte[]>();

        public bool ContainsHash(byte[] hash)
        {
            for (int i = 0; i < this.HashList.Count; ++i)
            {
                if (this.HashList[i].SequenceEqual(hash))
                {
                    return true;
                }
            }
            return false;
        }

        public CmpRes CompareHash(byte[] hash)
        {
            if (!this.HashList.Any())
            {
                return CmpRes.Uncertain;
            }
            else if (this.HashList.Count == 1)
            {
                return this.ContainsHash(hash) ? CmpRes.Matched : CmpRes.Mismatch;
            }
            byte[] first = this.HashList.First();
            if (!this.HashList.Skip(1).All(i => i.SequenceEqual(first)))
            {
                return CmpRes.Uncertain;
            }
            else
            {
                return first.SequenceEqual(hash) ? CmpRes.Matched : CmpRes.Mismatch;
            }
        }
    }

    internal class HashBasis
    {
        public HashBasis(string filePath)
        {
            this.UpdateWithFile(filePath);
        }

        public HashBasis() { }

        private bool AddHashAndName(string hashString, string name)
        {
            if (hashString is null || name is null)
            {
                return false;
            }
            if (CommonUtils.HashFromAnyString(hashString) is byte[] hash)
            {
                // Windows 文件名不区分大小写
                name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
                if (this.FileHashDict.ContainsKey(name))
                {
                    this.FileHashDict[name].HashList.Add(hash);
                }
                else
                {
                    this.FileHashDict[name] = new HashBasisDictValue(hash);
                }
                return true;
            }
            return false;
        }

        private bool AddOnlyHashString(string hashString)
        {
            return this.AddHashAndName(hashString, string.Empty);
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
                        string hashLine;
                        int readedLineCount = 0;
                        while ((hashLine = reader.ReadLine()) != null)
                        {
                            string[] items = hashLine.Split(
                                new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (items.Length == 2)
                            {
                                // 对应格式：hash-string *file-name
                                if (this.AddHashAndName(items[0], items[1]))
                                {
                                    ++readedLineCount;
                                }
                            }
                            else if (items.Length == 3)
                            {
                                // 对应格式：#hash-name *hash-string *file-name
                                if (this.AddHashAndName(items[1].Trim(new char[] { ' ', '*' }), items[2]))
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
                null : "收集校验依据失败，可能哈希值格式不正确";
            return this.ReasonForFailure;
        }

        public bool IsExpectedFileHash(string name, out byte[] outputHash)
        {
            string matchedName = null;
            foreach (string nameInMap in this.FileHashDict.Keys)
            {
                if (nameInMap.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    matchedName = nameInMap;
                    break;
                }
            }
            if (matchedName == null)
            {
                outputHash = default;
                return false;
            }
            HashBasisDictValue dictValue = this.FileHashDict[matchedName];
            dictValue.HasBeenChecked = true;
            if (!dictValue.HashList.Any())
            {
                outputHash = new byte[0];
                return true;
            }
            if (dictValue.HashList.Count == 1)
            {
                outputHash = dictValue.HashList[0];
                return true;
            }
            byte[] first = dictValue.HashList.First();
            if (!dictValue.HashList.Skip(1).All(i => i.SequenceEqual(first)))
            {
                outputHash = new byte[0];
                return true;
            }
            outputHash = first;
            return true;
        }

        public CmpRes Verify(string name, byte[] hash)
        {
            if (hash is null || name is null || !this.FileHashDict.Any())
            {
                return CmpRes.Unrelated;
            }
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
            if (this.FileHashDict.Keys.Count == 1 &&
                this.FileHashDict.Keys.Contains(string.Empty))
            {
                if (this.FileHashDict[string.Empty].ContainsHash(hash))
                {
                    return CmpRes.Matched;
                }
                else
                {
                    return CmpRes.Unrelated;
                }
            }
            if (!this.FileHashDict.TryGetValue(name, out HashBasisDictValue basisValue))
            {
                return CmpRes.Unrelated;
            }
            return basisValue.CompareHash(hash);
        }

        public string ReasonForFailure { get; private set; }

        public Dictionary<string, HashBasisDictValue> FileHashDict { get; }
            = new Dictionary<string, HashBasisDictValue>();
    }
}
