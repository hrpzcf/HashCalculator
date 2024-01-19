using System.Threading;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DoubleProgressModel : NotifiableModel
    {
        private string curFileName = null;
        private double curPercentage = 0;
        private int filesCount = 0;
        private int processedCount = 0;
        private RelayCommand cancelOperationCmd;
        private readonly bool isMarkFilesProgress;
        private bool isCancelled = false;

        public DoubleProgressModel(bool isMarkFilesModel)
        {
            this.isMarkFilesProgress = isMarkFilesModel;
        }

        public double WindowWidth
        {
            get
            {
                if (this.isMarkFilesProgress)
                {
                    return Settings.Current.MarkFilesProgressWidth;
                }
                else
                {
                    return Settings.Current.RestoreFilesProgressWidth;
                }
            }
            set
            {
                if (this.isMarkFilesProgress)
                {
                    Settings.Current.MarkFilesProgressWidth = value;
                }
                else
                {
                    Settings.Current.RestoreFilesProgressWidth = value;
                }
            }
        }

        public double WindowHeight
        {
            get
            {
                if (this.isMarkFilesProgress)
                {
                    return Settings.Current.MarkFilesProgressHeight;
                }
                else
                {
                    return Settings.Current.RestoreFilesProgressHeight;
                }
            }
            set
            {
                if (this.isMarkFilesProgress)
                {
                    Settings.Current.MarkFilesProgressHeight = value;
                }
                else
                {
                    Settings.Current.RestoreFilesProgressHeight = value;
                }
            }
        }

        public string CurFileName
        {
            get => this.curFileName;
            set => this.SetPropNotify(ref this.curFileName, value);
        }

        public double ProgressValue
        {
            get => this.curPercentage;
            set => this.SetPropNotify(ref this.curPercentage, value);
        }

        public int FilesCount
        {
            get => this.filesCount;
            set => this.SetPropNotify(ref this.filesCount, value);
        }

        public int ProcessedCount
        {
            get => this.processedCount;
            set => this.SetPropNotify(ref this.processedCount, value);
        }

        public bool IsCancelled
        {
            get => this.isCancelled;
            set => this.SetPropNotify(ref this.isCancelled, value);
        }

        public bool AutoClose { get; set; }

        public CancellationTokenSource TokenSrc { get; } = new CancellationTokenSource();

        private void CancelOperationAction(object param)
        {
            this.TokenSrc?.Cancel();
            this.IsCancelled = true;
        }

        public ICommand CancelOperationCmd
        {
            get
            {
                if (this.cancelOperationCmd == null)
                {
                    this.cancelOperationCmd = new RelayCommand(this.CancelOperationAction);
                }
                return this.cancelOperationCmd;
            }
        }
    }
}
