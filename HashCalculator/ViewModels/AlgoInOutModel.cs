using System.ComponentModel;
using System.Security.Cryptography;
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
                OutputType outputType;
                if (parent.SelectedOutputType != OutputType.Unknown)
                {
                    outputType = parent.SelectedOutputType;
                }
                else if (Settings.Current.SelectedOutputType != OutputType.Unknown)
                {
                    outputType = Settings.Current.SelectedOutputType;
                }
                else
                {
                    outputType = OutputType.BinaryLower;
                }
                CommonUtils.ClipboardSetText(HashDetailsWnd.This, BytesToStrByOutputTypeCvt.Convert(this.HashResult, outputType));
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
    }
}
