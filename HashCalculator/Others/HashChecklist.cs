using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HashCalculator
{
    internal class AlgHashMap
    {
        private bool FileIndependent { get; set; }

        private readonly Dictionary<string, List<byte[]>> algHashDict =
            new Dictionary<string, List<byte[]>>(StringComparer.OrdinalIgnoreCase);

        public bool IsExistingFile { get; set; }

        public AlgHashMap(string fileName, string algoName, byte[] hashBytes)
        {
            this.AddAlgHashMapOfFile(fileName, algoName, hashBytes);
        }

        public bool AddAlgHashMapOfFile(string fileName, string algoName, byte[] hashBytes)
        {
            this.FileIndependent = fileName == string.Empty;
            if (algoName == null || hashBytes == null)
            {
                return false;
            }
            if (this.algHashDict.TryGetValue(algoName, out var hashValues))
            {
                hashValues.Add(hashBytes);
            }
            else
            {
                this.algHashDict[algoName] = new List<byte[]>() { hashBytes };
            }
            return true;
        }

        public CmpRes CompareHash(string algoName, byte[] hashBytes)
        {
            if (algoName == null || hashBytes == null || !this.algHashDict.Any())
            {
                return CmpRes.Unrelated;
            }
            bool algoNameIndependent = false;
            if (!this.algHashDict.TryGetValue(algoName, out List<byte[]> hashValues))
            {
                algoNameIndependent = true;
                this.algHashDict.TryGetValue(string.Empty, out hashValues);
            }
            if (hashValues != null)
            {
                if (!hashValues.Any())
                {
                    return CmpRes.Uncertain;
                }
                if (algoNameIndependent)
                {
                    if (hashValues.Contains(hashBytes, BytesComparer.Default))
                    {
                        return CmpRes.Matched;
                    }
                    else
                    {
                        return CmpRes.Unrelated;
                    }
                }
                else
                {
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
            }
            return CmpRes.Unrelated;
        }

        public string[] GetExistingAlgoNames()
        {
            return this.algHashDict.Keys.Where(i => i != string.Empty).ToArray();
        }

        public int[] GetExistingDigestLengths()
        {
            return this.algHashDict.Values.SelectMany(i => i).Select(j => j.Length).Distinct().ToArray();
        }
    }

    internal class HashChecklist : IEnumerable<KeyValuePair<string, AlgHashMap>>
    {
        private static readonly Encoding encoding = Encoding.GetEncoding(
            "utf-8",
            new EncoderExceptionFallback(),
            new DecoderExceptionFallback());
        private static readonly char[] charsToTrim = { ' ', '*', '#', '\r', '\n' };

        public string ReasonForFailure { get; private set; }

        private readonly Dictionary<string, AlgHashMap> algHashMapOfFiles =
            new Dictionary<string, AlgHashMap>(StringComparer.OrdinalIgnoreCase);

        public HashChecklist() { }

        public HashChecklist(string filePath)
        {
            this.UpdateWithFile(filePath);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.algHashMapOfFiles.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, AlgHashMap>> GetEnumerator()
        {
            return this.algHashMapOfFiles.GetEnumerator();
        }

        public bool IsNameInChecklist(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && this.algHashMapOfFiles.ContainsKey(fileName))
            {
                this.algHashMapOfFiles[fileName].IsExistingFile = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 向校验依据对象添加一条以 <算法名>、<哈希字符串>、<文件名> 组成的依据
        /// </summary>
        private bool AddItem(string algoName, string hashString, string fileName)
        {
            if (!(algoName is null || hashString is null || fileName is null))
            {
                hashString = hashString.Trim(charsToTrim);
                if (CommonUtils.HashBytesFromString(hashString) is byte[] hash)
                {
                    algoName = algoName.Trim(charsToTrim);
                    fileName = fileName.Trim(charsToTrim);
                    if (this.algHashMapOfFiles.TryGetValue(fileName, out AlgHashMap algoHashMap))
                    {
                        algoHashMap.AddAlgHashMapOfFile(fileName, algoName, hash);
                    }
                    else
                    {
                        this.algHashMapOfFiles[fileName] = new AlgHashMap(fileName, algoName, hash);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool AddItemFromLine(string textLine)
        {
            string[] items = textLine.Split(
                new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            // 对应格式：hash-string
            if (items.Length == 1)
            {
                return this.AddItem(string.Empty, items[0], string.Empty);
            }
            // 对应格式：hash-string *file-fileName
            else if (items.Length == 2)
            {
                return this.AddItem(string.Empty, items[0], items[1]);
            }
            // 对应格式：#hash-name *hash-string *file-name
            else if (items.Length == 3 && this.AddItem(items[0], items[1], items[2]))
            {
                return true;
            }
            return false;
        }

        public string UpdateWithFile(string filePath)
        {
            this.ReasonForFailure = null;
            this.algHashMapOfFiles.Clear();
            try
            {
                using (StreamReader reader = new StreamReader(filePath, encoding, true))
                {
                    int addedLinesCount = 0;
                    string lineString = null;
                    while ((lineString = reader.ReadLine()) != null)
                    {
                        if (this.AddItemFromLine(lineString))
                        {
                            ++addedLinesCount;
                        }
                    }
                    if (addedLinesCount == 0)
                    {
                        this.ReasonForFailure = "没有搜集到依据，请检查校验依据文件内容";
                    }
                }
            }
            catch (DecoderFallbackException)
            {
                this.algHashMapOfFiles.Clear();
                this.ReasonForFailure = $"文件读取失败，仅支持兼容 UTF-8 编码的文本文件";
            }
            catch (Exception ex)
            {
                this.algHashMapOfFiles.Clear();
                this.ReasonForFailure = $"出现异常导致搜集校验依据失败，详情：\n{ex.Message}";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithText(string paragraph)
        {
            this.ReasonForFailure = null;
            this.algHashMapOfFiles.Clear();
            int readedLineCount = 0;
            foreach (string line in paragraph.Split('\r', '\n'))
            {
                if (this.AddItemFromLine(line))
                {
                    ++readedLineCount;
                }
            }
            if (readedLineCount == 0)
            {
                this.ReasonForFailure = "没有搜集到依据，请检查输入的文本段落内容";
            }
            return this.ReasonForFailure;
        }

        public bool TryGetAlgHashMapOfFile(string fileName, out AlgHashMap hashMap)
        {
            if (fileName == null)
            {
                hashMap = default(AlgHashMap);
                return false;
            }
            return this.algHashMapOfFiles.TryGetValue(fileName, out hashMap);
        }

        public AlgHashMap GetAlgHashMapOfFile(string fileName)
        {
            if (fileName == null || !this.algHashMapOfFiles.Any())
            {
                return default(AlgHashMap);
            }
            // Windows 文件名不区分大小写 (algHashMapOfFiles 使用了忽略大小写的比较器)
            if (this.algHashMapOfFiles.TryGetValue(fileName, out AlgHashMap algoHashMapOfFile))
            {
                return algoHashMapOfFile;
            }
            else if (this.algHashMapOfFiles.TryGetValue(string.Empty, out AlgHashMap algoHashMapOfNonFile))
            {
                return algoHashMapOfNonFile;
            }
            return default(AlgHashMap);
        }
    }
}
