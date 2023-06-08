using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    /// <summary>
    /// 文件哈希值校验的依据，此依据从文件解析而来
    /// </summary>
    internal class Basis
    {
        private readonly Dictionary<string, List<string>> nameHashs =
            new Dictionary<string, List<string>>();

        public void Clear()
        {
            this.nameHashs.Clear();
        }

        public bool Add(string[] hashName)
        {
            if (hashName.Length < 2 || hashName[0] == null || hashName[1] == null)
            {
                return false;
            }
            string hash = hashName[0].Trim().ToLower();
            // Windows 文件名不区分大小写
            string name = hashName[1].Trim(new char[] { '*', ' ', '\n' }).ToLower();
            if (this.nameHashs.ContainsKey(name))
            {
                this.nameHashs[name].Add(hash);
            }
            else
            {
                this.nameHashs[name] = new List<string> { hash };
            }
            return true;
        }

        public CmpRes Verify(string name, string hash)
        {
            if (hash == null || name == null || this.nameHashs.Count == 0)
            {
                return CmpRes.Unrelated;
            }
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
            hash = hash.Trim().ToLower();
            if (this.nameHashs.Keys.Count == 1
                && this.nameHashs.Keys.Contains(string.Empty))
            {
                if (this.nameHashs[string.Empty].Contains(hash))
                {
                    return CmpRes.Matched;
                }
                else
                {
                    return CmpRes.Unrelated;
                }
            }
            if (!this.nameHashs.TryGetValue(name, out List<string> hashs))
            {
                return CmpRes.Unrelated;
            }
            if (hashs.Count == 0)
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
