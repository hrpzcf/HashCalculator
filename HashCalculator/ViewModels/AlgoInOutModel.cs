using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;

namespace HashCalculator
{
    internal class AlgoInOutModel : NotifiableModel
    {
        private RelayCommand _copyHashResultCmd;
        private byte[] _hashResult;
        private CmpRes _hashCmpResult;
        private bool _export = false;
        private bool _selected = false;
        private bool _hashResultHandlerAdded = false;

        public AlgoInOutModel(IHashAlgoInfo algoInfo)
        {
            this.AlgoName = algoInfo.AlgoName;
            this.AlgoType = algoInfo.AlgoType;
            this.Algo = (HashAlgorithm)algoInfo;
            this.IAlgo = algoInfo;
        }

        public string AlgoName { get; }

        public AlgoType AlgoType { get; }

        public HashAlgorithm Algo { get; }

        public IHashAlgoInfo IAlgo { get; }

        public bool Export
        {
            get
            {
                return this._export;
            }
            set
            {
                this.SetPropNotify(ref this._export, value);
            }
        }

        public bool Selected
        {
            get
            {
                return this._selected;
            }
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
            get
            {
                return this._hashResult;
            }
            set
            {
                this.SetPropNotify(ref this._hashResult, value);
            }
        }

        public CmpRes HashCmpResult
        {
            get
            {
                return this._hashCmpResult;
            }
            set
            {
                this.SetPropNotify(ref this._hashCmpResult, value);
            }
        }

        public AlgoInOutModel NewAlgoInOutModel()
        {
            return new AlgoInOutModel(this.IAlgo.NewInstance());
        }

        public void SetHashResultChangedHandler(PropertyChangedEventHandler e)
        {
            if (!this._hashResultHandlerAdded)
            {
                this.PropertyChanged += e;
                this._hashResultHandlerAdded = true;
            }
        }

        private void CopyHashResultAction(object param)
        {
            if (this.HashResult != null && param is HashViewModel parent)
            {
                string format = Settings.Current.GenerateTextInFormat ?
                    Settings.Current.FormatForGenerateText : null;
                if (this.GenerateTextInFormat(parent, format, parent.SelectedOutputType, false,
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
        public string GenerateTextInFormat(HashViewModel parent, string format, OutputType output,
            bool endLine, bool casedAlgName)
        {
            if (parent != null && this.HashResult != null)
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
                    // "$algo$", "$hash$", "$path$", "$name$"
                    StringBuilder formatBuilder = new StringBuilder(format);
                    if (endLine)
                    {
                        formatBuilder.Append('\n');
                    }
                    formatBuilder.Replace("$algo$", algoName);
                    formatBuilder.Replace("$hash$", BytesToStrByOutputTypeCvt.Convert(this.HashResult, output));
                    formatBuilder.Replace("$name$", parent.FileInfo.Name);
                    formatBuilder.Replace("$path$", parent.FileInfo.FullName);
                    return formatBuilder.ToString();
                }
            }
            return default(string);
        }
    }
}
