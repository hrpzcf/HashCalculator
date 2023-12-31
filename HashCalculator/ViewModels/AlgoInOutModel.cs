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
                if (this.GenerateTextLineInFormat(parent,
                    Settings.Current.FormatForGenerateText, parent.SelectedOutputType) is string text)
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

        public string GenerateTextLineInFormat(HashViewModel parent, string format, OutputType outputType)
        {
            if (parent != null && this.HashResult != null)
            {
                if (outputType == OutputType.Unknown)
                {
                    if (parent.SelectedOutputType != OutputType.Unknown)
                    {
                        outputType = parent.SelectedOutputType;
                    }
                    else
                    {
                        outputType = Settings.Current.SelectedOutputType;
                    }
                }
                if (!Settings.Current.GenerateTextInFormat || string.IsNullOrEmpty(format))
                {
                    return BytesToStrByOutputTypeCvt.Convert(this.HashResult, outputType);
                }
                else
                {
                    // "$algo$", "$hash$", "$path$", "$name$"
                    StringBuilder formatBuilder = new StringBuilder(format);
                    formatBuilder.Replace("$algo$", this.AlgoName);
                    formatBuilder.Replace("$hash$", BytesToStrByOutputTypeCvt.Convert(this.HashResult, outputType));
                    formatBuilder.Replace("$name$", parent.FileInfo.Name);
                    formatBuilder.Replace("$path$", parent.FileInfo.FullName);
                    return formatBuilder.ToString();
                }
            }
            return default(string);
        }
    }
}
