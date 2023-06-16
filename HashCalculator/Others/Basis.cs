using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace HashCalculator
{
    internal class BasisDictValue
    {
        public BasisDictValue() { }

        public BasisDictValue(byte[] initHash)
        {
            this.HashList.Add(initHash);
        }

        public bool IsExplored { get; set; }

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
                return this.ContainsHash(hash)
                    ? CmpRes.Matched : CmpRes.Mismatch;
            }
            byte[] first = this.HashList.First();
            if (!this.HashList.Skip(1).All(i => i.SequenceEqual(first)))
            {
                return CmpRes.Uncertain;
            }
            else
            {
                return first.SequenceEqual(hash)
                    ? CmpRes.Matched : CmpRes.Mismatch;
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

        public Dictionary<string, BasisDictValue> NameHashMap { get; }
            = new Dictionary<string, BasisDictValue>();

        public bool UpdateWithFile(string filePath)
        {
            this.NameHashMap.Clear();
            try
            {
                foreach (string line in File.ReadAllLines(filePath))
                {
                    string[] items = line.Split(
                        new char[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length < 2)
                    {
                        if (MessageBox.Show(
                            "读取哈希值文件时遇到格式不正确的行：\n" +
                            "选择 [是] 忽略该行并从下一行开始读取，选择 [否] 放弃读取。",
                            "错误",
                            MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            return false;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    this.AddHashAndName(items);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"校验依据文件打开失败：\n{ex.Message}", "错误");
            }
            return false;
        }

        public bool UpdateWithHash(string hash)
        {
            this.NameHashMap.Clear();
            this.AddHashAndName(new string[] { hash.Trim(), string.Empty });
            return true;
        }

        private bool AddHashAndName(string[] hashAndName)
        {
            if (hashAndName.Length < 2 || hashAndName.Any(i => i is null))
            {
                return false;
            }
            if (CommonUtils.GuessFromAnyHashString(hashAndName[0]) is byte[] hash)
            {
                // Windows 文件名不区分大小写
                string name = hashAndName[1].Trim(new char[] { '*', ' ', '\n' }).ToLower();
                if (this.NameHashMap.ContainsKey(name))
                {
                    this.NameHashMap[name].HashList.Add(hash);
                }
                else
                {
                    this.NameHashMap[name] = new BasisDictValue(hash);
                }
                return true;
            }
            return false;
        }

        public CmpRes Verify(string name, byte[] hash)
        {
            if (hash is null || name is null || !this.NameHashMap.Any())
            {
                return CmpRes.Unrelated;
            }
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
            if (this.NameHashMap.Keys.Count == 1 &&
                this.NameHashMap.Keys.Contains(string.Empty))
            {
                if (this.NameHashMap[string.Empty].ContainsHash(hash))
                {
                    return CmpRes.Matched;
                }
                else
                {
                    return CmpRes.Unrelated;
                }
            }
            if (!this.NameHashMap.TryGetValue(name, out BasisDictValue basisValue))
            {
                return CmpRes.Unrelated;
            }
            return basisValue.CompareHash(hash);
        }

        public bool IsExpectedFileHash(string name, out byte[] outputHash)
        {
            string matchedName = null;
            foreach (string nameInMap in this.NameHashMap.Keys)
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
            BasisDictValue basisDictValue = this.NameHashMap[matchedName];
            basisDictValue.IsExplored = true;
            if (!basisDictValue.HashList.Any())
            {
                outputHash = new byte[0];
                return true;
            }
            if (basisDictValue.HashList.Count == 1)
            {
                outputHash = basisDictValue.HashList[0];
                return true;
            }
            byte[] first = basisDictValue.HashList.First();
            if (!basisDictValue.HashList.Skip(1).All(i => i.SequenceEqual(first)))
            {
                outputHash = new byte[0];
                return true;
            }
            outputHash = first;
            return true;
        }
    }
}
