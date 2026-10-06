using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;

namespace HashCalculator;

public class AlgoInOutModel : BaseViewModel
{
    private CmpRes _hashCmpResult;
    private byte[] _hashResult;
    private bool _export = false;
    private bool _selected = false;
    private string _algorithmAlias = null;
    private string[] _aliasWords = null;
    private readonly string _defaultAlias = null;
    private RelayCommand _copyHashResultCmd;

    private static readonly string[] separators = new string[]
    {
        ",",
        "\n",
        "\r\n",
    };

    public AlgoInOutModel(IHashAlgoInfo algoInfo, string defaultAlias) :
        this(algoInfo, defaultAlias, alias: null)
    {
    }

    public AlgoInOutModel(IHashAlgoInfo algoInfo, string defaultAlias, string alias)
    {
        this.IAlgo = algoInfo;
        this.AlgoName = algoInfo.AlgoName;
        this.AlgoType = algoInfo.AlgoType;
        this.Algo = (HashAlgorithm)algoInfo;
        this._defaultAlias = defaultAlias;
        this.AlgorithmAlias = alias ?? defaultAlias;
    }

    public string AlgoName { get; }

    public AlgoType AlgoType { get; }

    public HashAlgorithm Algo { get; }

    public IHashAlgoInfo IAlgo { get; }

    public bool Export
    {
        get => this._export;
        set => this.SetPropNotify(ref this._export, value);
    }

    public bool Selected
    {
        get => this._selected;
        set
        {
            // AlgoGroupModel 类根据此属性的变化计数
            // 所以新值与旧值没有区别则不能触发通知，否则计数就不正确
            if (value != this._selected)
            {
                this.SetPropNotify(ref this._selected, value);
            }
        }
    }

    public byte[] HashResult
    {
        get => this._hashResult;
        set => this.SetPropNotify(ref this._hashResult, value);
    }

    public CmpRes HashCmpResult
    {
        get => this._hashCmpResult;
        set => this.SetPropNotify(ref this._hashCmpResult, value);
    }

    public string AlgorithmAlias
    {
        get => this._algorithmAlias;
        set
        {
            // 值未变则不通知；值变了要作废分词缓存
            if (this.AlgorithmAlias?.Equals(value) != true)
            {
                this._algorithmAlias = value;
                this._aliasWords = null;
                this.NotifyPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 别名的分词结果（按逗号/换行拆分并去除首尾空白），供匹配用，只在第一次被读取时拆分并缓存。<br/>
    /// 之所以按需计算：别名匹配只服务于"把算法名解析成 AlgoType"这一件事，而该事只在
    /// AlgorithmsModel.ProvidedAlgos 上展开；表格里每个文件对应的行模型数量可达数十万，从不读取本属性。
    /// 若在构造时为每个实例都预备一份分词结果（数组 + 惰性迭代器 + 属性变更订阅），会有几十 MB 的无谓占用。
    /// </summary>
    private string[] AliasWords
    {
        get
        {
            return this._aliasWords ??= string.IsNullOrEmpty(this._algorithmAlias) ?
                Array.Empty<string>() :
                this._algorithmAlias.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim()).ToArray();
        }
    }

    public AlgoInOutModel NewAlgoInOutModel()
    {
        return new AlgoInOutModel(this.IAlgo.NewInstance(), this.AlgorithmAlias,
            this._defaultAlias);
    }

    /// <summary>
    /// 指定名称是否为本算法的别名之一。一个算法可以有多个用逗号/换行分隔的等价别名。
    /// </summary>
    public bool IsMyAliasWord(string alias, StringComparer comparer)
    {
        string[] words = this.AliasWords;
        for (int i = 0; i < words.Length; ++i)
        {
            if (comparer.Equals(words[i], alias))
            {
                return true;
            }
        }
        return false;
    }

    public void ResetAlias()
    {
        this.AlgorithmAlias = this._defaultAlias;
    }

    private void CopyHashResultAction(object param)
    {
        if (this.HashResult != null && param is HashViewModel parent)
        {
            string format = Settings.Current.GenerateTextInFormat ?
                Settings.Current.FormatForGenerateText : null;
            if (this.GenerateTextInFormat(
                parent, format, parent.SelectedOutputType, endLine: false, seeExport: false,
                Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
            {
                CommonUtils.ClipboardSetText(text);
                NotificationSender.SnackbarSuccess($"已按模板复制当前哈希值：{text}");
            }
        }
    }

    public ICommand CopyHashResultCmd
    {
        get
        {
            this._copyHashResultCmd ??= new RelayCommand(this.CopyHashResultAction);
            return this._copyHashResultCmd;
        }
    }

    /// <summary>
    /// 参数 format 为 null 或空字符串代表不按格式生成字符串。<br/>
    /// 参数 output 为 OutputType.Unknown 代表按 parent.SelectedOutputType 格式化哈希值，<br/>
    /// 如果 parent.SelectedOutputType 也是 OutputType.Unknown，则使用 Settings.Current.SelectedOutputType。
    /// </summary>
    internal string GenerateTextInFormat(HashViewModel parent, string format, OutputType output,
        bool endLine, bool seeExport, bool casedAlgName)
    {
        if (parent != null && !parent.Arguments.IsInvalidName && this.HashResult != null &&
            (!seeExport || this.Export))
        {
            if (output == OutputType.Unknown)
            {
                if (parent.SelectedOutputType != OutputType.Unknown)
                {
                    output = parent.SelectedOutputType;
                }
                else
                {
                    output = Settings.Current.SelectedOutputType;
                }
            }
            string lineBreak = Settings.Current.UseUnixStyleLineBreaks ? "\n" : "\r\n";
            if (string.IsNullOrEmpty(format))
            {
                string text = BytesToStrByOutputTypeCvt.Convert(this.HashResult, output);
                return endLine ? $"{text}{lineBreak}" : text;
            }
            else
            {
                string algoName = this.AlgoName;
                if (casedAlgName)
                {
                    algoName = output switch
                    {
                        OutputType.BinaryLower => algoName.ToLower(),
                        _ => algoName.ToUpper(),
                    };
                }
                StringBuilder formatBuilder = new StringBuilder(format);
                if (endLine)
                {
                    formatBuilder.Append(lineBreak);
                }
                formatBuilder.Replace("$horztab$", "\t");
                formatBuilder.Replace("$newline$", lineBreak);
                formatBuilder.Replace("$algo$", algoName);
                formatBuilder.Replace("$hash$", BytesToStrByOutputTypeCvt.Convert(this.HashResult, output));
                formatBuilder.Replace("$name$", parent.Information.Name);
                formatBuilder.Replace("$path$", parent.Arguments.Deprecated ?
                    parent.Information.Name : parent.Information.FullName);
                formatBuilder.Replace("$relpath$", parent.RelativePath);
                formatBuilder.Replace("$filesize$", parent.FileLength.ToString());
                return formatBuilder.ToString();
            }
        }
        return default(string);
    }
}
