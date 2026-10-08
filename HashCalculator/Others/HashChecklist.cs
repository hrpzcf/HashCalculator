using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator;

public class HashChecker
{
    private bool fileIndependent;

    /// <summary>
    /// 已记录的条目数，即下面两个数组的有效长度。
    /// </summary>
    private int itemCount;

    /// <summary>
    /// 已记录的算法，与哈希值数组同下标一一对应（同一算法可能重复出现）。
    /// </summary>
    private AlgoType[] algoTypes = Array.Empty<AlgoType>();

    /// <summary>
    /// 已记录的哈希值，与算法数组同下标一一对应。
    /// </summary>
    private byte[][] hashByteLists = Array.Empty<byte[]>();

    public bool IsExistingFile { get; set; }

    public HashChecker() { }

    public HashChecker(string relpath, AlgoType algo, byte[] hashBytes)
    {
        this.AddCheckerItem(relpath, algo, hashBytes);
    }

    public void AddCheckerItem(string relpath, AlgoType algo, byte[] hashBytes)
    {
        if (this.itemCount == this.algoTypes.Length)
        {
            int capacity = this.itemCount == 0 ? 4 : this.itemCount * 2;
            Array.Resize(ref this.algoTypes, capacity);
            Array.Resize(ref this.hashByteLists, capacity);
        }
        this.algoTypes[this.itemCount] = algo;
        this.hashByteLists[this.itemCount] = hashBytes;
        this.fileIndependent = relpath == string.Empty;
        ++this.itemCount;
    }

    public CmpRes ComparisonResultOf(AlgoType algoType, byte[] hashBytes)
    {
        if (hashBytes == null || this.itemCount == 0)
        {
            return CmpRes.Unrelated;
        }
        // 同一算法可能记录了多个哈希值，彼此不一致时说明清单本身不可信
        int firstIndex = -1;
        for (int i = 0; i < this.itemCount; ++i)
        {
            if (this.algoTypes[i] != algoType)
            {
                continue;
            }
            if (firstIndex < 0)
            {
                firstIndex = i;
            }
            else if (!BytesComparer.Default.Equals(this.hashByteLists[firstIndex], this.hashByteLists[i]))
            {
                return CmpRes.Uncertain;
            }
        }
        if (firstIndex < 0)
        {
            // 清单里没有记录该算法时，用记录为 UNKNOWN（算法未知）的哈希值判断
            for (int i = 0; i < this.itemCount; ++i)
            {
                if (this.algoTypes[i] == AlgoType.UNKNOWN &&
                    BytesComparer.Default.Equals(this.hashByteLists[i], hashBytes))
                {
                    return CmpRes.Matched;
                }
            }
            return CmpRes.Unrelated;
        }
        return BytesComparer.Default.Equals(this.hashByteLists[firstIndex], hashBytes) ?
            CmpRes.Matched : this.fileIndependent ? CmpRes.Unrelated : CmpRes.Mismatch;
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
        HashSet<AlgoType> existingAlgoTypes = new HashSet<AlgoType>();
        for (int i = 0; i < this.itemCount; ++i)
        {
            if (this.algoTypes[i] != AlgoType.UNKNOWN)
            {
                existingAlgoTypes.Add(this.algoTypes[i]);
            }
        }
        return existingAlgoTypes;
    }

    public HashSet<int> GetExistingDigestLengths()
    {
        HashSet<int> existingDigestLengths = new HashSet<int>();
        for (int i = 0; i < this.itemCount; ++i)
        {
            existingDigestLengths.Add(this.hashByteLists[i].Length);
        }
        return existingDigestLengths;
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

    /// <summary>
    /// 缓冲允许等待解析的字符数上限（安全阀）。<br/>
    /// 缓冲里保留的只是"还没解析完的部分"：正常情况下只有最后一条成功匹配之后的一小段，
    /// 远达不到这个量；只有整份文件都没有换行、或某条记录本身极大的文件才会逼近它。
    /// 到顶时按"解析不了"处理并报错，避免缓冲无限增长。
    /// </summary>
    private const int ChecklistWaitChars = 32 * 1024 * 1024;

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
            if (_supportedEncodings == null)
            {
                PrepareSupportedEncodings();
            }
            // 逐个候选编码充当探测：解码失败会抛 DecoderFallbackException（可能出现在读取中途），
            // 此时清掉已解析出的内容并换下一个编码；能读到底就采用这个编码，不再试后面的。
            // 所以 ExtendChecklistWithReader 不需要返回值：它不抛异常就代表"这份编码可用"。
            foreach (Encoding encoding in _supportedEncodings)
            {
                using (StreamReader reader = new StreamReader(filePath, encoding, true,
                    ChecklistReadChars))
                {
                    try
                    {
                        this.ExtendChecklistWithReader(reader, fileExt);
                        return this.ReasonForFailure;
                    }
                    catch (DecoderFallbackException)
                    {
                        this.Initialize();
                    }
                }
            }
            this.ReasonForFailure = "只支持 UTF8/ANSI/UNICODE/GB18030 及兼容编码的校验信息文件。";
        }
        catch (Exception ex)
        {
            this.fileHashCheckerDict?.Clear();
            this.ReasonForFailure = $"出现异常导致解析校验信息失败：\n{ex.Message}";
        }
        return this.ReasonForFailure;
    }

    /// <summary>
    /// 分块读取并解析校验信息，避免整份文本常驻内存：
    /// 每块累积到 <see cref="ChecklistChunkChars"/> 个字符就立即解析并丢弃。
    /// </summary>
    private void ExtendChecklistWithReader(StreamReader reader, string fileExt)
    {
        // 解析方案只取决于扩展名与设置，与文件内容无关：先判空，省掉整份文件的读取与逐块切字符串
        ChecklistParserSession parserSession = new ChecklistParserSession(fileExt);
        if (parserSession.IsEmpty)
        {
            this.ReasonForFailure = "没有可用的校验信息解析方案。";
            return;
        }
        int readCount;
        char[] readBuffer = new char[ChecklistReadChars];
        StringBuilder chunkBuilder = new StringBuilder(ChecklistChunkChars);
        // 上次解析时缓冲的长度。这里用"缓冲又攒够一块"（而不是"缓冲长度超过一块"）决定是否解析：
        // 解析时会把还没匹配上的内容留在缓冲里（见 ExtendChecklistWithChunk），缓冲会长期大于一块，
        // 若按长度判断，则每读入一小段（ChecklistReadChars）都会重新解析整段，并切出一份与缓冲等长的
        // 大字符串（4M 字符约 8 MB，会进大对象堆），白白产生大对象
        int parsedLength = 0;
        // 直接按字符块读取而不是用 ReadLine：后者会为每一行分配一个临时字符串，
        // 数十万行的文件会产生同量级的短命垃圾，加重 GC 负担
        while ((readCount = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            chunkBuilder.Append(readBuffer, 0, readCount);
            if (chunkBuilder.Length - parsedLength < ChecklistChunkChars)
            {
                continue;
            }
            if (!this.ExtendChecklistWithChunk(chunkBuilder, parserSession))
            {
                return;   // 等待解析的内容已到上限，ReasonForFailure 已设置
            }
            parsedLength = chunkBuilder.Length;   // 解析后缓冲已变短，按当前长度重新计时
        }
        if (chunkBuilder.Length != 0)
        {
            // 文件已读完，不存在"下一块"：剩余内容必须整块解析，
            // 否则被留给下一块补全的最后一行会因为没有下一块而永远得不到解析
            this.ExtendChecklistWithChunk(chunkBuilder, parserSession, isLastChunk: true);
        }
        // 走到这里说明方案列表不为空（为空时已在开头返回）：没有任何方案匹配上内容时给出提示
        if (!parserSession.HasSettled)
        {
            this.ReasonForFailure = "没有解析到校验信息，请检查解析方案或校验信息文件内容。";
        }
    }

    /// <summary>
    /// 解析缓冲中已累积的一块，把已解析完的部分移除。<br/>
    /// 非最后一块只交完整行（残行交过去可能匹配出被截断的假记录）；只消费到最后一个成功匹配的结束位置，
    /// 之后的内容留给下一块（那里可能有下一条记录的起始部分）。<br/>
    /// 最后一块（<paramref name="isLastChunk"/> 为 true）必须整块解析，否则文件最后一行永远得不到解析。<br/>
    /// 返回 false 表示等待解析的内容超过上限（失败原因已设置），读取应当中止。
    /// </summary>
    private bool ExtendChecklistWithChunk(StringBuilder chunkBuilder,
        ChecklistParserSession parserSession, bool isLastChunk = false)
    {
        // 默认整块：最后一块之后不会再读到内容，剩余内容都要解析
        int lineEnd = chunkBuilder.Length;
        if (!isLastChunk)
        {
            // 非最后一块只把"已经完整的内容"交给解析器：
            // 把还没读到换行的残行交过去，会让以 $ 结尾的解析方案在文本末尾匹配成功，
            // 产出一条被截断的假记录
            int lastNewline = -1;
            for (int i = chunkBuilder.Length - 1; i >= 0; --i)
            {
                if (chunkBuilder[i] == '\n')
                {
                    lastNewline = i;
                    break;
                }
            }
            if (lastNewline < 0)
            {
                // 整个缓冲里一个换行符都没有：无法区分"内容到此结束"和"内容被切断"，
                // 先不解析也不消费，等下一块（读到换行或读到文件末尾时再解析）
                return this.TryAccumulateMoreContent(chunkBuilder, 0);
            }
            lineEnd = lastNewline + 1;
        }
        string lines = chunkBuilder.ToString(0, lineEnd);
        // 只消费到最后一个成功匹配的结束位置：之后的内容留给下一块，
        // 那里可能有下一条记录的起始部分；一条都没匹配上时也全部留着
        int consumed = parserSession.Extend(lines, this);
        if (isLastChunk)
        {
            // 最后一块之后不会再读到内容：不管匹配到哪，剩下的都已经解析过，整块消费
            consumed = lineEnd;
        }
        else if (!this.TryAccumulateMoreContent(chunkBuilder, consumed))
        {
            return false;
        }
        chunkBuilder.Remove(0, consumed);
        return true;
    }

    /// <summary>
    /// 判断缓冲还能不能继续等待更多内容（等待量 = 缓冲长度 − 已消费位置）：超过
    /// <see cref="ChecklistWaitChars"/> 时设置失败原因并返回 false，避免缓冲无限增长。
    /// </summary>
    private bool TryAccumulateMoreContent(StringBuilder chunkBuilder, int consumed)
    {
        if (chunkBuilder.Length - consumed > ChecklistWaitChars)
        {
            this.ReasonForFailure = "校验信息文件里存在超长单行内容，无法解析。";
            return false;
        }
        return true;
    }

    /// <summary>
    /// 校验信息的解析方案会话：未定居前逐块尝试各个方案，定居后只用定居的方案。<br/>
    /// 一个校验信息文件只含一种格式，方案一经选定就用于余下各块，不再改变。
    /// </summary>
    private sealed class ChecklistParserSession
    {
        private readonly TemplateForChecklistModel[] parsers;
        private TemplateForChecklistModel settledParser = null;

        public ChecklistParserSession(string fileExt)
        {
            // 物化成数组：方案只需确定一次，也可以在读文件之前就判断有没有可用方案
            this.parsers = GetParsers(fileExt).ToArray();
        }

        public bool IsEmpty => this.parsers.Length == 0;

        public bool HasSettled => this.settledParser != null;

        /// <summary>
        /// 解析一段文本，返回其中已消费到的字符位置；返回 0 表示这段文本里没有可解析的内容。
        /// </summary>
        public int Extend(string lines, HashChecklist checklist)
        {
            if (this.settledParser != null)
            {
                this.settledParser.ExtendChecklistWithLines(lines, checklist, out int consumed);
                return consumed;
            }
            foreach (TemplateForChecklistModel parser in this.parsers)
            {
                if (parser.ExtendChecklistWithLines(lines, checklist, out int consumedWhenSettled))
                {
                    this.settledParser = parser;
                    return consumedWhenSettled;
                }
            }
            return 0;
        }
    }

    public string UpdateWithText(string paragraph)
    {
        bool anyPaser = false;
        bool anyItemAdded = false;
        this.Initialize();
        foreach (TemplateForChecklistModel parser in GetParsers(null))
        {
            anyPaser = true;
            if (parser.ExtendChecklistWithLines(paragraph, this, out _))
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
            this.ReasonForFailure = "没有解析到校验信息，请检查解析方案或输入的文本内容。";
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
