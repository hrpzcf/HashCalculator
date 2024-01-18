using System.Threading;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DoubleProgressModel : NotifiableModel
    {
        private string curFileName;
        private double curPercentage = 0;
        private int filesCount = 0;
        private int processedCount = 0;
        private RelayCommand cancelOperationCmd;
        private bool isCancelled = false;

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
