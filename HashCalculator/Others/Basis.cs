using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace HashCalculator
{
    internal class BasisDictValueWrapper
    {
        public BasisDictValueWrapper() { }

        public BasisDictValueWrapper(byte[] initHash)
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

    /// <summary>
    /// 文件哈希值校验的校验工具
    /// </summary>
    internal class Basis
    {
        public Basis(string filePath)
        {
            this.UpdateWithFile(filePath);
        }

        public Basis() { }

        public Dictionary<string, BasisDictValueWrapper> FileHashDict { get; }
            = new Dictionary<string, BasisDictValueWrapper>();

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
                    this.FileHashDict[name] = new BasisDictValueWrapper(hash);
                }
                return true;
            }
            return false;
        }

        public bool UpdateWithHash(string hash)
        {
            this.FileHashDict.Clear();
            this.AddHashAndName(hash.Trim(), string.Empty);
            return true;
        }

        public bool UpdateWithFile(string filePath)
        {
            this.FileHashDict.Clear();
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string hashLine;
                        while ((hashLine = reader.ReadLine()) != null)
                        {
                            string[] items = hashLine.Split(
                                new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (items.Length == 2)
                            {
                                // 旧导出格式：hash-string *file-name
                                this.AddHashAndName(items[0], items[1]);
                            }
                            else if (items.Length == 3)
                            {
                                // 新导出格式：#SHA-1 *hash-string *file-name
                                string hashString = items[1].Trim(new char[] { '*' });
                                this.AddHashAndName(hashString, items[2]);
                            }
                            else
                            {
                                if (MessageBox.Show(
                                        "读取哈希值文件时遇到格式不正确的行：\n选择 [是] 忽略该行并从下一行开始读取，选择 [否] 全部放弃。",
                                        "错误",
                                        MessageBoxButton.YesNo) == MessageBoxResult.No)
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"校验依据文件打开或读取失败：\n{ex.Message}", "错误");
            }
            return false;
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
            if (!this.FileHashDict.TryGetValue(name, out BasisDictValueWrapper basisValue))
            {
                return CmpRes.Unrelated;
            }
            return basisValue.CompareHash(hash);
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
            BasisDictValueWrapper dictValue = this.FileHashDict[matchedName];
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
    }
}
