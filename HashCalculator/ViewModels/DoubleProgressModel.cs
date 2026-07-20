using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DoubleProgressModel : BaseViewModel
    {
        private bool isCancelled = false;
        private string windowTitle = string.Empty;
        private double currentValue = 0;
        private string currentString = null;
        private int totalCount = 0;
        private int processedCount = 0;
        private string totalString = null;
        private Visibility subProgressVisibility;
        private Visibility totalProgressVisibility;
        private Visibility totalStringVisibility;
        private RelayCommand cancelOperationCmd;

        public DoubleProgressModel() { }

        public DoubleProgressModel(string title)
        {
            this.windowTitle = title;
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set => this.SetPropNotify(ref this.windowTitle, value);
        }

        public double CurrentValue
        {
            get => this.currentValue;
            set => this.SetPropNotify(ref this.currentValue, value);
        }

        public string CurrentString
        {
            get => this.currentString;
            set => this.SetPropNotify(ref this.currentString, value);
        }

        public int TotalCount
        {
            get => this.totalCount;
            set
            {
                this.SetPropNotify(ref this.totalCount, value);
                if (string.IsNullOrEmpty(this.totalString))
                {
                    this.NotifyPropertyChanged(nameof(this.TotalString));
                }
            }
        }

        public int ProcessedCount
        {
            get => this.processedCount;
            set
            {
                this.SetPropNotify(ref this.processedCount, value);
                if (string.IsNullOrEmpty(this.totalString))
                {
                    this.NotifyPropertyChanged(nameof(this.TotalString));
                }
            }
        }

        public string TotalString
        {
            get
            {
                if (!string.IsNullOrEmpty(this.totalString))
                {
                    return this.totalString;
                }
                else
                {
                    return $"正在处理第 {this.ProcessedCount}/{this.TotalCount} 个...";
                }
            }

            set => this.SetPropNotify(ref this.totalString, value);
        }

        public Visibility SubProgressVisibility
        {
            get => this.subProgressVisibility;
            set => this.SetPropNotify(ref this.subProgressVisibility, value);
        }

        public Visibility TotalProgressVisibility
        {
            get => this.totalProgressVisibility;
            set => this.SetPropNotify(ref this.totalProgressVisibility, value);
        }

        public Visibility TotalStringVisibility
        {
            get => this.totalStringVisibility;
            set => this.SetPropNotify(ref this.totalStringVisibility, value);
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
                this.cancelOperationCmd ??= new RelayCommand(this.CancelOperationAction);
                return this.cancelOperationCmd;
            }
        }
    }
}
