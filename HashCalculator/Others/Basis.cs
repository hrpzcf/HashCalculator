using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace HashCalculator
{
    /// <summary>
    /// 文件哈希值校验的校验工具
    /// </summary>
    internal class Basis
    {
        private readonly Dictionary<string, List<string>> nameHashsMap
            = new Dictionary<string, List<string>>();

        public Window Parent { get; set; }

        public bool UpdateWithFile(string filePath)
        {
            this.nameHashsMap.Clear();
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
                            this.Parent,
                            "哈希值文件行读取错误，可能该行格式不正确，继续？",
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
                MessageBox.Show(
                    this.Parent, $"校验依据来源文件打开失败：\n{ex.Message}", "错误");
            }
            return false;
        }

        public bool UpdateWithHash(string hash)
        {
            this.nameHashsMap.Clear();
            this.AddHashAndName(new string[] { hash.Trim(), string.Empty });
            return true;
        }

        private bool AddHashAndName(string[] hashName)
        {
            if (hashName.Length < 2 || hashName[0] == null || hashName[1] == null)
            {
                return false;
            }
            string hash = hashName[0].Trim().ToLower();
            // Windows 文件名不区分大小写
            string name = hashName[1].Trim(new char[] { '*', ' ', '\n' }).ToLower();
            if (this.nameHashsMap.ContainsKey(name))
            {
                this.nameHashsMap[name].Add(hash);
            }
            else
            {
                this.nameHashsMap[name] = new List<string> { hash };
            }
            return true;
        }

        public CmpRes Verify(string name, string hash)
        {
            if (hash == null || name == null || !this.nameHashsMap.Any())
            {
                return CmpRes.Unrelated;
            }
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
            hash = hash.Trim().ToLower();
            if (this.nameHashsMap.Keys.Count == 1
                && this.nameHashsMap.Keys.Contains(string.Empty))
            {
                if (this.nameHashsMap[string.Empty].Contains(hash))
                {
                    return CmpRes.Matched;
                }
                else
                {
                    return CmpRes.Unrelated;
                }
            }
            if (!this.nameHashsMap.TryGetValue(name, out List<string> hashs))
            {
                return CmpRes.Unrelated;
            }
            if (!hashs.Any())
            {
                return CmpRes.Uncertain;
            }
            if (hashs.Count > 1)
            {
                string fst = hashs.First();
                if (hashs.All(i => i == fst))
                {
                    return fst == hash ? CmpRes.Matched : CmpRes.Mismatch;
                }
                else
                {
                    return CmpRes.Uncertain;
                }
            }
            return hashs.Contains(hash) ? CmpRes.Matched : CmpRes.Mismatch;
        }
    }
}
