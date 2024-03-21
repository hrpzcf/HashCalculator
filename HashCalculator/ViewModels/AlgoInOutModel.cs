using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;

namespace HashCalculator
{
    public class AlgoInOutModel : NotifiableModel
    {
        private CmpRes _hashCmpResult;
        private byte[] _hashResult;
        private bool _export = false;
        private bool _selected = false;
        private bool _hashResultHandlerAdded = false;
        private string[] _algorithmAlias = null;
        private readonly string[] _presetAlias = null;
        private RelayCommand _copyHashResultCmd;

        public AlgoInOutModel(IHashAlgoInfo algoInfo, string[] preset)
        {
            this.IAlgo = algoInfo;
            this.AlgoName = algoInfo.AlgoName;
            this.AlgoType = algoInfo.AlgoType;
            this.Algo = (HashAlgorithm)algoInfo;
            this._presetAlias = preset;
            this.AlgorithmAlias = preset;
        }

        public AlgoInOutModel(IHashAlgoInfo algoInfo, string[] preset, string[] alias)
        {
            this.IAlgo = algoInfo;
            this.AlgoName = algoInfo.AlgoName;
            this.AlgoType = algoInfo.AlgoType;
            this.Algo = (HashAlgorithm)algoInfo;
            this._presetAlias = preset;
            this.AlgorithmAlias = alias;
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

        public string[] AlgorithmAlias
        {
            get => this._algorithmAlias;
            set => this.SetPropNotify(ref this._algorithmAlias, value);
        }

        public AlgoInOutModel NewAlgoInOutModel()
        {
            return new AlgoInOutModel(this.IAlgo.NewInstance(), this.AlgorithmAlias,
                this._presetAlias);
        }

        public void ResetAlias()
        {
            this.AlgorithmAlias = this._presetAlias;
        }

        public void SetHashResultChangedHandler(PropertyChangedEventHandler handler)
        {
            if (!this._hashResultHandlerAdded)
            {
                this.PropertyChanged += handler;
                this._hashResultHandlerAdded = true;
            }
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
                }
            }
        }

        public ICommand CopyHashResultCmd
        {
            get
            {
                if (this._copyHashResultCmd == null)
                {
                    this._copyHashResultCmd = new RelayCommand(this.CopyHashResultAction);
                }
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
                if (string.IsNullOrEmpty(format))
                {
                    string text = BytesToStrByOutputTypeCvt.Convert(this.HashResult, output);
                    return endLine ? $"{text}\n" : text;
                }
                else
                {
                    string algoName = this.AlgoName;
                    if (casedAlgName)
                    {
                        switch (output)
                        {
                            case OutputType.BinaryLower:
                                algoName = algoName.ToLower();
                                break;
                            case OutputType.BinaryUpper:
                                algoName = algoName.ToUpper();
                                break;
                        }
                    }
                    // "$algo$", "$hash$", "$path$", "$name$", "$relpath$"
                    StringBuilder formatBuilder = new StringBuilder(format);
                    if (endLine)
                    {
                        formatBuilder.Append('\n');
                    }
                    formatBuilder.Replace("$algo$", algoName);
                    formatBuilder.Replace("$hash$", BytesToStrByOutputTypeCvt.Convert(this.HashResult, output));
                    formatBuilder.Replace("$name$", parent.Information.Name);
                    formatBuilder.Replace("$path$", parent.Arguments.Deprecated ?
                        parent.Information.Name : parent.Information.FullName);
                    formatBuilder.Replace("$relpath$", parent.RelativePath);
                    return formatBuilder.ToString();
                }
            }
            return default(string);
        }
    }
}
