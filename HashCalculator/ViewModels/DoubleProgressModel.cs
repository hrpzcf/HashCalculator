using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal delegate double GetWndSize();

    internal delegate void SetWndSize(double size);

    internal class SizeDelegates
    {
        public SizeDelegates() { }

        public SizeDelegates(GetWndSize gww, SetWndSize sww, GetWndSize gwh, SetWndSize swh)
        {
            this.GetWindowWidth = gww;
            this.SetWindowWidth = sww;
            this.GetWindowHeight = gwh;
            this.SetWindowHeight = swh;
        }

        public GetWndSize GetWindowWidth { get; set; }

        public SetWndSize SetWindowWidth { get; set; }

        public GetWndSize GetWindowHeight { get; set; }

        public SetWndSize SetWindowHeight { get; set; }
    }

    internal class DoubleProgressModel : NotifiableModel
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

        public DoubleProgressModel(SizeDelegates delegates)
        {
            this.SizeDelegates = delegates;
        }

        public DoubleProgressModel(string title, SizeDelegates delegates)
        {
            this.windowTitle = title;
            this.SizeDelegates = delegates;
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set => this.SetPropNotify(ref this.windowTitle, value);
        }

        public SizeDelegates SizeDelegates { get; set; }

        public double WindowWidth
        {
            get
            {
                return this.SizeDelegates?.GetWindowWidth?.Invoke() ?? 400.0;
            }
            set
            {
                this.SizeDelegates?.SetWindowWidth?.Invoke(value);
            }
        }

        public double WindowHeight
        {
            get
            {
                return this.SizeDelegates?.GetWindowHeight?.Invoke() ?? 200.0;
            }
            set
            {
                this.SizeDelegates?.SetWindowHeight?.Invoke(value);
            }
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
            get
            {
                return this.totalCount;
            }
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
            get
            {
                return this.processedCount;
            }
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
                    return $"正在处理第 {this.ProcessedCount}/{this.TotalCount} 个......";
                }
            }
            set
            {
                this.SetPropNotify(ref this.totalString, value);
            }
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
                if (this.cancelOperationCmd == null)
                {
                    this.cancelOperationCmd = new RelayCommand(this.CancelOperationAction);
                }
                return this.cancelOperationCmd;
            }
        }
    }
}
