using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HashCalculator
{
    internal class HashChecker
    {
        private bool fileIndependent;

        private readonly Dictionary<AlgoType, List<byte[]>> algoHashDict =
            new Dictionary<AlgoType, List<byte[]>>();

        public bool IsExistingFile { get; set; }

        public HashChecker(string relpath, string algoName, byte[] hashBytes)
        {
            this.AddCheckerItem(relpath, algoName, hashBytes);
        }

        public bool AddCheckerItem(string relpath, string algoName, byte[] hashBytes)
        {
            if (relpath != null && algoName != null && hashBytes != null &&
                AlgosPanelModel.TryGetAlgoType(algoName, out AlgoType algorithm))
            {
                this.fileIndependent = relpath == string.Empty;
                if (this.algoHashDict.TryGetValue(algorithm, out var hashValues))
                {
                    hashValues.Add(hashBytes);
                }
                else
                {
                    this.algoHashDict[algorithm] = new List<byte[]>() { hashBytes };
                }
                return true;
            }
            return false;
        }

        public CmpRes GetCheckResult(AlgoType algoType, byte[] hashBytes)
        {
            if (hashBytes == null || !this.algoHashDict.Any())
            {
                return CmpRes.Unrelated;
            }
            bool algoTypeIndependent = false;
            if (!this.algoHashDict.TryGetValue(algoType, out List<byte[]> hashValues))
            {
                algoTypeIndependent = true;
                this.algoHashDict.TryGetValue(AlgoType.Unknown, out hashValues);
            }
            if (hashValues != null)
            {
                if (!hashValues.Any())
                {
                    return CmpRes.Uncertain;
                }
                if (algoTypeIndependent)
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
                        CmpRes.Matched : this.fileIndependent ? CmpRes.Unrelated : CmpRes.Mismatch;
                }
            }
            return CmpRes.Unrelated;
        }

        public void SetModelCheckResult(HashViewModel model)
        {
            if (model != null && model.AlgoInOutModels != null)
            {
                foreach (AlgoInOutModel inOut in model.AlgoInOutModels)
                {
                    inOut.HashCmpResult = this.GetCheckResult(inOut.AlgoType, inOut.HashResult);
                }
            }
        }

        public AlgoType[] GetExistingAlgoTypes()
        {
            return this.algoHashDict.Keys.Where(i => i != AlgoType.Unknown).ToArray();
        }

        public int[] GetExistingDigestLengths()
        {
            return this.algoHashDict.Values.SelectMany(i => i).Select(j => j.Length).Distinct().ToArray();
        }
    }

    internal class HashChecklist : IEnumerable<KeyValuePair<string, HashChecker>>
    {
        private static readonly char[] directorySeparators =
            new char[] { '/', '\\' };
        private static readonly Encoding[] supportedEncodings = new Encoding[]
        {
            Encoding.GetEncoding(Encoding.UTF8.CodePage,
                new EncoderExceptionFallback(),
                new DecoderExceptionFallback()),
            Encoding.GetEncoding(Encoding.Default.CodePage,
                new EncoderExceptionFallback(),
                new DecoderExceptionFallback()),
            Encoding.GetEncoding("gb18030",
                new EncoderExceptionFallback(),
                new DecoderExceptionFallback()),
            Encoding.GetEncoding(Encoding.Unicode.CodePage,
                new EncoderExceptionFallback(),
                new DecoderExceptionFallback()),
        };

        private Dictionary<string, HashChecker> fileHashCheckerDict = null;

        public static HashChecklist Text(string text)
        {
            HashChecklist checklist = new HashChecklist();
            checklist.UpdateWithText(text);
            return checklist;
        }

        public static HashChecklist File(string filePath)
        {
            HashChecklist checklist = new HashChecklist();
            checklist.UpdateWithFile(filePath);
            return checklist;
        }

        public static HashChecklist Checklist(HashChecklist old)
        {
            HashChecklist checklist = new HashChecklist();
            checklist.UpdateWithChecklist(old);
            return checklist;
        }

        public HashChecklist() { }

        public string ReasonForFailure { get; private set; }

        public bool KeysAreRelativePaths { get; private set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.fileHashCheckerDict.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, HashChecker>> GetEnumerator()
        {
            return this.fileHashCheckerDict.GetEnumerator();
        }

        private void Initialize()
        {
            this.ReasonForFailure = null;
            this.KeysAreRelativePaths = false;
            if (this.fileHashCheckerDict == null)
            {
                this.fileHashCheckerDict = new Dictionary<string, HashChecker>(
                    StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                this.fileHashCheckerDict.Clear();
            }
        }

        public bool IsNameInChecklist(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && this.fileHashCheckerDict.ContainsKey(fileName))
            {
                this.fileHashCheckerDict[fileName].IsExistingFile = true;
                return true;
            }
            return false;
        }

        public bool AddChecklistItem(string algo, string hash, string relpath)
        {
            if (algo == null || hash == null || relpath == null)
            {
                return false;
            }
            if (CommonUtils.HashBytesFromString(hash) is byte[] hashBytes)
            {
                relpath = relpath.Replace('/', '\\');
                if (this.fileHashCheckerDict.TryGetValue(relpath, out HashChecker checker))
                {
                    checker.AddCheckerItem(relpath, algo, hashBytes);
                }
                else
                {
                    this.fileHashCheckerDict[relpath] = new HashChecker(relpath, algo, hashBytes);
                }
                return true;
            }
            return false;
        }

        private IEnumerable<TemplateForChecklistModel> GetParsers(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                foreach (var model in Settings.Current.TemplatesForChecklist)
                {
                    if (model.ContainsExtension(extension))
                    {
                        yield return model;
                    }
                }
            }
            foreach (TemplateForChecklistModel template in Settings.Current.TemplatesForChecklist)
            {
                if (template.ContainsExtension(null))
                {
                    yield return template;
                }
            }
        }

        public string UpdateWithFile(string filePath)
        {
            this.Initialize();
            try
            {
                bool contentDecoded = false;
                foreach (Encoding encoding in supportedEncodings)
                {
                    using (StreamReader reader = new StreamReader(filePath, encoding, true))
                    {
                        string checklistLines;
                        try
                        {
                            checklistLines = reader.ReadToEnd();
                        }
                        catch (DecoderFallbackException)
                        {
                            continue;
                        }
                        contentDecoded = true;
                        bool anyPaser = false;
                        bool anyItemAdded = false;
                        string fileExt = Path.GetExtension(filePath);
                        foreach (TemplateForChecklistModel parser in this.GetParsers(fileExt))
                        {
                            anyPaser = true;
                            if (parser.ExtendChecklistWithLines(checklistLines, this))
                            {
                                anyItemAdded = true;
                                break;
                            }
                        }
                        if (!anyPaser)
                        {
                            this.ReasonForFailure = "没有可用的校验依据解析方案";
                        }
                        else if (!anyItemAdded)
                        {
                            this.ReasonForFailure = "没有搜集到依据，请检查校验依据文件内容";
                        }
                        break;
                    }
                }
                if (!contentDecoded)
                {
                    this.ReasonForFailure = "只支持 UTF8/UTF16/ANSI/GB18030 及其兼容编码的校验依据文件";
                }
            }
            catch (Exception ex)
            {
                this.fileHashCheckerDict?.Clear();
                this.ReasonForFailure = $"出现异常导致搜集校验依据失败：\n{ex.Message}";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithText(string paragraph)
        {
            bool anyPaser = false;
            bool anyItemAdded = false;
            this.Initialize();
            foreach (TemplateForChecklistModel parser in this.GetParsers(null))
            {
                anyPaser = true;
                if (parser.ExtendChecklistWithLines(paragraph, this))
                {
                    anyItemAdded = true;
                    break;
                }
            }
            if (!anyPaser)
            {
                this.ReasonForFailure = "没有可用的校验依据解析方案";
            }
            else if (!anyItemAdded)
            {
                this.ReasonForFailure = "没有搜集到依据，请检查输入的文本内容";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithChecklist(HashChecklist checklist)
        {
            this.fileHashCheckerDict = checklist.fileHashCheckerDict;
            this.ReasonForFailure = checklist.ReasonForFailure;
            this.KeysAreRelativePaths = checklist.KeysAreRelativePaths;
            checklist.fileHashCheckerDict = null;
            checklist.Initialize();
            return this.ReasonForFailure;
        }

        public bool TryGetFileHashChecker(string mapKey, out HashChecker checker)
        {
            if (mapKey != null)
            {
                if (!this.KeysAreRelativePaths)
                {
                    mapKey = Path.GetFileName(mapKey);
                }
                return this.fileHashCheckerDict.TryGetValue(mapKey, out checker);
            }
            checker = null;
            return false;
        }

        public bool TryGetFileOrEmptyHashChecker(string mapKey, out HashChecker checker)
        {
            if (mapKey != null && this.fileHashCheckerDict.Any())
            {
                if (!this.KeysAreRelativePaths)
                {
                    mapKey = Path.GetFileName(mapKey);
                }
                return this.fileHashCheckerDict.TryGetValue(mapKey, out checker)
                    || this.fileHashCheckerDict.TryGetValue(string.Empty, out checker);
            }
            checker = null;
            return false;
        }

        /// <summary>
        /// 断言 HashChecklist 里所有文件标识都是相对路径并取得所有文件完整路径
        /// </summary>
        public bool AssertRelativeGetFull(string rootDir, out IEnumerable<string> fullPaths)
        {
            if (!string.IsNullOrWhiteSpace(rootDir) && this.fileHashCheckerDict.Any())
            {
                // 如果所有 Key 都不含 "\" 或 "/" 则进一步判断是否是相对路径
                if (!this.fileHashCheckerDict.Keys.Any(i => i.IndexOfAny(directorySeparators) != -1))
                {
                    List<string> filePathList = new List<string>();
                    foreach (KeyValuePair<string, HashChecker> pair in this.fileHashCheckerDict)
                    {
                        string filePath = Path.Combine(rootDir, pair.Key);
                        if (!System.IO.File.Exists(filePath))
                        {
                            // 如果有一个路径找不到文件，则视所有 Key 为非相对路径，
                            // 返回 false 和 null 结果，意味着调用者需要自行枚举文件找到目标
                            fullPaths = null;
                            return false;
                        }
                        filePathList.Add(filePath);
                    }
                    // 如果所有 Key 与 parentDir 连接都能找到文件，也视所有 Key 为相对路径
                    fullPaths = filePathList;
                    this.KeysAreRelativePaths = true;
                    return true;
                }
                // 只要某个 Key 含有一个 "\" 或 "/" 则视所有 Key 为相对路径，不管文件是否存在
                else
                {
                    fullPaths = this.fileHashCheckerDict.Keys.Select(rel => Path.Combine(rootDir, rel));
                    this.KeysAreRelativePaths = true;
                    return true;
                }
            }
            fullPaths = null;
            return false;
        }
    }
}
