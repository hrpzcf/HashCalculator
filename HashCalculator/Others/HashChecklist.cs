using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator
{
    public class HashChecker
    {
        private bool fileIndependent;

        private readonly Dictionary<AlgoType, List<byte[]>> algoHashDict =
            new Dictionary<AlgoType, List<byte[]>>();

        public bool IsExistingFile { get; set; }

        public HashChecker() { }

        public HashChecker(string relpath, AlgoType algoType, byte[] hashBytes)
        {
            this.AddCheckerItem(relpath, algoType, hashBytes);
        }

        public void AddCheckerItem(string relpath, AlgoType algo, byte[] hashBytes)
        {
            if (this.algoHashDict.TryGetValue(algo, out List<byte[]> value))
            {
                value.Add(hashBytes);
            }
            else
            {
                this.algoHashDict[algo] = new List<byte[]>() { hashBytes };
            }
            this.fileIndependent = relpath == string.Empty;
        }

        public CmpRes ComparisonResultOf(AlgoType algoType, byte[] hashBytes)
        {
            if (hashBytes == null || this.algoHashDict.Count == 0)
            {
                return CmpRes.Unrelated;
            }
            bool algoTypeIndependent = false;
            if (!this.algoHashDict.TryGetValue(algoType, out List<byte[]> hashValues))
            {
                algoTypeIndependent = true;
                this.algoHashDict.TryGetValue(AlgoType.UNKNOWN, out hashValues);
            }
            if (hashValues != null)
            {
                if (hashValues.Count == 0)
                {
                    return CmpRes.Uncertain;
                }
                if (algoTypeIndependent)
                {
                    return hashValues.Contains(hashBytes, BytesComparer.Default) ?
                        CmpRes.Matched : CmpRes.Unrelated;
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

        public void SetComparisonResult(HashViewModel model)
        {
            if (model != null && model.AlgoInOutModels != null)
            {
                foreach (AlgoInOutModel inOut in model.AlgoInOutModels)
                {
                    inOut.HashCmpResult = this.ComparisonResultOf(inOut.AlgoType, inOut.HashResult);
                }
            }
        }

        public HashSet<AlgoType> GetExistingAlgoTypes()
        {
            return this.algoHashDict.Keys.Where(i => i != AlgoType.UNKNOWN).ToHashSet();
        }

        public HashSet<int> GetExistingDigestLengths()
        {
            return this.algoHashDict.Values.SelectMany(i => i).Select(j => j.Length).ToHashSet();
        }
    }

    public class HashChecklist : IEnumerable<KeyValuePair<string, HashChecker>>
    {
        /// <summary>
        /// 每次从文件读取的字符数，同时用作 StreamReader 的内部缓冲大小。
        /// 取较大值是为了摊薄读取与解码的固定开销（StreamReader 的默认缓冲只有 1024 字符）。
        /// </summary>
        private const int ChecklistReadChars = 64 * 1024;

        /// <summary>
        /// 分块解析校验信息文件时每块累积的字符数。
        /// 块越大正则匹配次数越少，但块字符串及其构建缓冲会同时驻留，故在匹配效率与峰值内存之间取折中。
        /// </summary>
        private const int ChecklistChunkChars = 4 * 1024 * 1024;

        private static Encoding[] _supportedEncodings = null;
        private static readonly char[] directorySeparators = new char[] { '/', '\\' };
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

        public static HashChecklist File(string filePath, IEnumerable<AlgoType> algoTypes)
        {
            HashChecklist checklist = new HashChecklist();
            checklist.AlgoTypesFromOption = algoTypes?.ToArray();
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

        public AlgoType AlgoTypeFromFileExt { get; set; } =
            AlgoType.UNKNOWN;

        public AlgoType[] AlgoTypesFromOption { get; set; }

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
            if (!string.IsNullOrEmpty(fileName) &&
                this.fileHashCheckerDict.TryGetValue(fileName, out HashChecker value))
            {
                value.IsExistingFile = true;
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
            if (!AlgorithmsModel.TryGetAlgoType(algo, out AlgoType algoType) ||
                algoType == AlgoType.UNKNOWN)
            {
                algoType = this.AlgoTypeFromFileExt;
            }
            if (CommonUtils.HashBytesFromString(hash) is byte[] hashBytes)
            {
                relpath = relpath.Replace('/', '\\');
                if (!this.fileHashCheckerDict.TryGetValue(relpath, out HashChecker hashChecker))
                {
                    hashChecker = new HashChecker();
                    this.fileHashCheckerDict[relpath] = hashChecker;
                }
                // 从校验信息文件内容和扩展名推断 algoType 后，无论是否仍是 UNKNOWN，都添加到 HashChecker
                hashChecker.AddCheckerItem(relpath, algoType, hashBytes);
                // 如果 algoType 仍然是 UNKNOWN，则假定解析校验信息所得哈希值对应算法与启动校验时指定的算法相同
                // 遍历启动校验时用户指定的算法，如该算法的摘要长度与 hashBytes 长度相同则添加到 HashChecker
                if (algoType == AlgoType.UNKNOWN && this.AlgoTypesFromOption?.Length > 0)
                {
                    foreach (AlgoType fallbackAlgoType in this.AlgoTypesFromOption)
                    {
                        if (fallbackAlgoType.DigestLength() == hashBytes.Length)
                        {
                            hashChecker.AddCheckerItem(relpath, fallbackAlgoType, hashBytes);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private static IEnumerable<TemplateForChecklistModel> GetParsers(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                foreach (TemplateForChecklistModel model in Settings.Current.TemplatesForChecklist)
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

        private static void PrepareSupportedEncodings()
        {
            // 用于兼容 .NET Core 及以上版本，避免找不到 GB18030 等编码。
            // 注册 CodePagesEncodingProvider.Instance 后，
            // 在 Windows 上， GetEncoding(0) 返回与系统的活动代码页匹配的编码，
            // 该代码与 .NET Framework 中的行为相同。
            // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // 上一句改为在 App.StartupHandler 方法处执行一次即可。
            _supportedEncodings = new Encoding[] {
                Encoding.GetEncoding(Encoding.UTF8.CodePage,
                        new EncoderExceptionFallback(),
                        new DecoderExceptionFallback()),
                // ANSI 兼容编码，返回值依赖于系统的活动代码页，见上注释
                Encoding.GetEncoding(0, new EncoderExceptionFallback(),
                        new DecoderExceptionFallback()),
                Encoding.GetEncoding(Encoding.Unicode.CodePage,
                        new EncoderExceptionFallback(),
                        new DecoderExceptionFallback()),
                Encoding.GetEncoding("GB18030", new EncoderExceptionFallback(),
                        new DecoderExceptionFallback()),
            };
        }

        public string UpdateWithFile(string filePath)
        {
            this.Initialize();
            try
            {
                string fileExt = Path.GetExtension(filePath);
                if (AlgorithmsModel.TryGetAlgoType(fileExt.TrimStart('.'),
                    out AlgoType algoType))
                {
                    this.AlgoTypeFromFileExt = algoType;
                }
                bool contentDecoded = false;
                if (_supportedEncodings == null)
                {
                    PrepareSupportedEncodings();
                }
                foreach (Encoding encoding in _supportedEncodings)
                {
                    using (StreamReader reader = new StreamReader(filePath, encoding, true,
                        ChecklistReadChars))
                    {
                        try
                        {
                            this.ExtendChecklistWithReader(reader, fileExt);
                            contentDecoded = true;
                        }
                        catch (DecoderFallbackException)
                        {
                            // 分块读取时解码失败可能出现在中途，此前已解析出的内容必须清掉
                            this.Initialize();
                            continue;
                        }
                        break;
                    }
                }
                if (!contentDecoded)
                {
                    this.ReasonForFailure = "只支持 UTF8/ANSI/UNICODE/GB18030 及兼容编码的校验信息文件。";
                }
            }
            catch (Exception ex)
            {
                this.fileHashCheckerDict?.Clear();
                this.ReasonForFailure = $"出现异常导致搜集校验信息失败：\n{ex.Message}";
            }
            return this.ReasonForFailure;
        }

        /// <summary>
        /// 分块读取并解析校验信息，避免整份文本常驻内存：
        /// 每块累积到 <see cref="ChecklistChunkChars"/> 个字符就立即解析并丢弃。
        /// </summary>
        private void ExtendChecklistWithReader(StreamReader reader, string fileExt)
        {
            int readCount;
            char[] readBuffer = new char[ChecklistReadChars];
            TemplateForChecklistModel settledParser = null;
            StringBuilder chunkBuilder = new StringBuilder(ChecklistChunkChars);
            List<TemplateForChecklistModel> parsers = GetParsers(fileExt).ToList();
            // 直接按字符块读取而不是用 ReadLine：后者会为每一行分配一个临时字符串，
            // 数十万行的文件会产生同量级的短命垃圾，加重 GC 负担
            while ((readCount = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                chunkBuilder.Append(readBuffer, 0, readCount);
                if (chunkBuilder.Length >= ChecklistChunkChars)
                {
                    ExtendChecklistWithChunk(chunkBuilder, ref settledParser, parsers);
                }
            }
            if (chunkBuilder.Length != 0)
            {
                ExtendChecklistWithChunk(chunkBuilder, ref settledParser, parsers);
            }
            if (parsers.Count == 0)
            {
                this.ReasonForFailure = "没有可用的校验信息解析方案。";
            }
            else if (settledParser == null)
            {
                this.ReasonForFailure = "没有搜集到校验信息，请检查校验信息文件内容。";
            }
        }

        /// <summary>
        /// 解析缓冲中已累积的一块，并把块尾最后一条完整行及其后的残行留给下一块：
        /// 跨行的解析方案（.SUMS/.HASH）需要上一行与当前行在同一块内才能匹配，残行则等后续补全后再解析。
        /// </summary>
        private void ExtendChecklistWithChunk(StringBuilder chunkBuilder,
            ref TemplateForChecklistModel settledParser, List<TemplateForChecklistModel> parsers)
        {
            // 从末尾往回找第二个换行符，切在它之后：它之后的内容就是最后一条完整行及其后的残行
            int cutIndex = 0;
            int newlineCount = 0;
            for (int i = chunkBuilder.Length - 1; i >= 0; --i)
            {
                if (chunkBuilder[i] == '\n' && ++newlineCount == 2)
                {
                    cutIndex = i + 1;
                    break;
                }
            }
            if (cutIndex == 0)
            {
                // 块内不足两条完整行（例如单行就超过块大小）时无法再往后推，只能整块解析
                cutIndex = chunkBuilder.Length;
            }
            foreach (TemplateForChecklistModel parser in parsers)
            {
                // 一个校验信息文件只含一种格式，方案一经选定就用于余下各块，不再改变
                if (settledParser != null && !ReferenceEquals(parser, settledParser))
                {
                    continue;
                }
                if (parser.ExtendChecklistWithLines(chunkBuilder.ToString(0, cutIndex), this))
                {
                    settledParser = parser;
                    break;
                }
            }
            chunkBuilder.Remove(0, cutIndex);
        }

        public string UpdateWithText(string paragraph)
        {
            bool anyPaser = false;
            bool anyItemAdded = false;
            this.Initialize();
            foreach (TemplateForChecklistModel parser in GetParsers(null))
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
                this.ReasonForFailure = "没有可用的校验信息解析方案。";
            }
            else if (!anyItemAdded)
            {
                this.ReasonForFailure = "没有搜集到校验信息，请检查输入的文本内容。";
            }
            return this.ReasonForFailure;
        }

        public string UpdateWithChecklist(HashChecklist checklist)
        {
            this.fileHashCheckerDict = checklist.fileHashCheckerDict;
            this.AlgoTypesFromOption = checklist.AlgoTypesFromOption;
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

        public bool TryGetFileOrEmptyStrHashChecker(string mapKey, out HashChecker checker)
        {
            if (mapKey != null && this.fileHashCheckerDict.Count != 0)
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
            if (!string.IsNullOrWhiteSpace(rootDir) && this.fileHashCheckerDict.Count != 0)
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
