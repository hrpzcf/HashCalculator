using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class AppViewModel : INotifyPropertyChanged
    {
        private delegate void AddModelsSource(ModelArg arg);
        private readonly AddModelsSource AHVMS;
        private QueueState queueState = QueueState.None;
        private string hashCheckReport = string.Empty;
        private bool noDurationColumn;
        private bool noExportColumn;
        private static readonly Dispatcher MainDispatcher
            = Application.Current.Dispatcher;
        private CancellationTokenSource cancellation;
        private int _currentSerialNumber = 0;
        private int _finishedNumberInQueue = 0;
        private int _totalNumberInQueue = 0;
        private List<ModelArg> droppedFiles = new List<ModelArg>();
        private static TaskList queueCleaners;
        private readonly BlockingCollection<HashViewModel> hashViewModelsQueue
            = new BlockingCollection<HashViewModel>();
        private static readonly object _concurrentLock = new object();
        private static readonly object _serialLock = new object();
        private static readonly object _checkNumberLock = new object();
        private static readonly object _queueNumberLock = new object();

        public static AppViewModel Instance;

        public bool NoExportColumn
        {
            get { return this.noExportColumn; }
            set { this.noExportColumn = value; this.OnPropertyChanged(); }
        }

        public bool NoDurationColumn
        {
            get { return this.noDurationColumn; }
            set { this.noDurationColumn = value; this.OnPropertyChanged(); }
        }

        public string Report
        {
            set
            {
                this.hashCheckReport = value;
                this.OnPropertyChanged();
            }
            get
            {
                if (this.hashCheckReport == string.Empty)
                    return "暂无校验报告...";
                else
                    return this.hashCheckReport;
            }
        }

        public int FinishedInQueue
        {
            get
            {
                lock (_queueNumberLock)
                    return this._finishedNumberInQueue;
            }
            set
            {
                lock (_queueNumberLock)
                    this._finishedNumberInQueue = value;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<HashViewModel> HashViewModels { get; }
            = new ObservableCollection<HashViewModel>();

        public QueueState State
        {
            get => this.queueState;
            set
            {
                if ((this.queueState == QueueState.None
                    || this.queueState == QueueState.Stopped)
                    && value == QueueState.Started)
                {
                    MainDispatcher.Invoke(() => { this.Report = string.Empty; });
                    this.queueState = value;
                    this.OnPropertyChanged();
                }
                else if (this.queueState == QueueState.Started && value == QueueState.Stopped)
                {
                    this.GenerateVerificationReport();
                    this.queueState = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int TotalNumberInQueue
        {
            get
            {
                lock (_queueNumberLock)
                    return this._totalNumberInQueue;
            }
            set
            {
                lock (_queueNumberLock)
                    this._totalNumberInQueue = value;
                this.OnPropertyChanged();
            }
        }

        public AppViewModel()
        {
            queueCleaners = new TaskList(8, this.ComputeMonitor);
            this.AHVMS = new AddModelsSource(this.AddViewModelsSource);
            Instance = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void AfterQueueItemsCountChanges()
        {
            lock (_checkNumberLock)
            {
                if (this.TotalNumberInQueue > 0 && this.FinishedInQueue == 0 && this.State != QueueState.Started)
                {
                    MainDispatcher.Invoke(() => { this.State = QueueState.Started; });
                }
                else if (this.TotalNumberInQueue > 0 && this.FinishedInQueue == this.TotalNumberInQueue)
                {
                    MainDispatcher.Invoke(() =>
                    {
                        this.FinishedInQueue = this.TotalNumberInQueue = 0;
                        this.State = QueueState.Stopped;
                    });
                }
#if DEBUG
                else if (this.FinishedInQueue < 0 || this.TotalNumberInQueue < 0)
                {
                    Console.WriteLine($"[异常] 已完成任务：{this.FinishedInQueue}，总数：{this.TotalNumberInQueue}");
                    throw new InvalidOperationException("已完成数和总数异常");
                }
#endif
            }
        }

        private void CanbeStartSubscriber(HashViewModel model)
        {
            if (!this.hashViewModelsQueue.Contains(model))
            {
                this.hashViewModelsQueue.Add(model);
            }
#if DEBUG
            else
            {
                Console.WriteLine("当前队列已包含相同的元素");
            }
#endif
        }

        private void ComputeMonitor(object obj)
        {
            HashViewModel model;
            CancellationToken token = (CancellationToken)obj;
            while (true)
            {
                try
                {
                    model = this.hashViewModelsQueue.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                model.ComputeHashValue();
            }
        }

        private void DecreaseQueueTotal(int number)
        {
            if (number < 0)
                number = 0;
            this.TotalNumberInQueue -= number;
            this.AfterQueueItemsCountChanges();
        }

        private void AddViewModelsSource(ModelArg arg)
        {
            int modelSerial = this.SerialGet();
            HashViewModel model = new HashViewModel(modelSerial, arg);
            model.ModelCanbeStartEvent += this.CanbeStartSubscriber;
            model.ComputeFinishedEvent += this.IncreaseQueueFinished;
            model.WaitingModelCanceledEvent += this.DecreaseQueueTotal;
            this.HashViewModels.Add(model);
        }

        private void DisplayModels(IEnumerable<ModelArg> args, CancellationToken token)
        {
            int argsCount = args.Count();
            if (token.IsCancellationRequested || argsCount == 0)
                return;
            MainDispatcher.Invoke(() => { this.IncreaseQueueTotal(argsCount); });
            foreach (ModelArg arg in args)
            {
                if (token.IsCancellationRequested)
                {
                    MainDispatcher.Invoke(() => { this.DecreaseQueueTotal(argsCount); });
                    break;
                }
                MainDispatcher.Invoke(this.AHVMS, arg);
                --argsCount;
            }
            foreach (HashViewModel model in this.HashViewModels)
                model.StartupModel(true);
        }

        private void IncreaseQueueFinished(int number)
        {
            if (number < 0)
                number = 0;
            this.FinishedInQueue += number;
            this.AfterQueueItemsCountChanges();
        }

        private void IncreaseQueueTotal(int number)
        {
            if (number < 0)
                number = 0;
            this.TotalNumberInQueue += number;
            this.AfterQueueItemsCountChanges();
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int SerialGet()
        {
            lock (_serialLock) { return ++this._currentSerialNumber; }
        }

        private void SerialReset()
        {
            lock (_serialLock) { this._currentSerialNumber = 0; }
        }

        public static void SetConcurrent(SimCalc num)
        {
            lock (_concurrentLock)
            {
                switch (num)
                {
                    case SimCalc.One:
                        queueCleaners.Adjust(1);
                        break;
                    case SimCalc.Two:
                        queueCleaners.Adjust(2);
                        break;
                    case SimCalc.Four:
                        queueCleaners.Adjust(4);
                        break;
                    case SimCalc.Eight:
                        queueCleaners.Adjust(8);
                        break;
                }
            }
        }

        public void SetColumnVisibility(bool noExportColumn, bool noDurationColumn)
        {
            this.NoExportColumn = noExportColumn;
            this.NoDurationColumn = noDurationColumn;
        }

        public void ClearHashViewModels()
        {
            this.droppedFiles.Clear();
            this.SerialReset();
            this.HashViewModels.Clear();
        }

        public void GenerateVerificationReport()
        {
            int noresult, unrelated, matched, mismatch, uncertain, succeeded, canceled, hasFailed;
            noresult = unrelated = matched = mismatch = uncertain = succeeded = canceled = hasFailed = 0;
            foreach (HashViewModel hm in this.HashViewModels)
            {
                switch (hm.CmpResult)
                {
                    case CmpRes.NoResult:
                        ++noresult;
                        break;
                    case CmpRes.Unrelated:
                        ++unrelated;
                        break;
                    case CmpRes.Matched:
                        ++matched;
                        break;
                    case CmpRes.Mismatch:
                        ++mismatch;
                        break;
                    case CmpRes.Uncertain:
                        ++uncertain;
                        break;
                }
                switch (hm.Result)
                {
                    case HashResult.Succeeded:
                        ++succeeded;
                        break;
                    case HashResult.Canceled:
                        ++canceled;
                        break;
                    case HashResult.HasFailed:
                        ++hasFailed;
                        break;
                }
            }
            this.Report
                = $"校验报告：\n\n已匹配：{matched}\n"
                + $"不匹配：{mismatch}\n"
                + $"不确定：{uncertain}\n"
                + $"无关联：{unrelated}\n"
                + $"未校验：{noresult} \n\n"
                + $"队列总数：{this.HashViewModels.Count}\n"
                + $"已成功：{succeeded}\n"
                + $"已失败：{hasFailed}\n"
                + $"已取消：{canceled}";
        }

        public void Models_PauseOne(HashViewModel model)
        {
            model.PauseOrContinueModel(PauseMode.Invert);
        }

        public void Models_PauseAll()
        {
            foreach (var model in this.HashViewModels)
                model.PauseOrContinueModel(PauseMode.Pause);
        }

        public void Models_ContinueAll()
        {
            foreach (var model in this.HashViewModels)
                model.PauseOrContinueModel(PauseMode.Continue);
        }

        public void Models_CancelAll()
        {
            this.cancellation?.Cancel();
            this.cancellation?.Dispose();
            foreach (var model in this.HashViewModels)
                model.ShutdownModel();
            this.cancellation = GlobalCancellation.Handler;
        }

        public void Models_CancelOne(HashViewModel model)
        {
            model.ShutdownModel();
        }

        public void Models_Restart(bool newLines)
        {
            if (!newLines)
            {
                bool forceStart = !Settings.Current.RecalculateIncomplete;
                int canbeStartModelCount = 0;
                foreach (var model in this.HashViewModels)
                {
                    if (model.StartupModel(forceStart))
                        ++canbeStartModelCount;
                }
                this.IncreaseQueueTotal(canbeStartModelCount);
            }
            else
            {
                if (this.droppedFiles.Count <= 0)
                    return;
                List<ModelArg> args = this.droppedFiles;
                this.droppedFiles = new List<ModelArg>();
                this.QueueDisplayModels(args);
            }
        }

        public void Models_StartOne(HashViewModel viewModel)
        {
            this.IncreaseQueueTotal(1);
            viewModel.StartupModel(true);
        }

        public void QueueDisplayModels(IEnumerable<ModelArg> args)
        {
            this.droppedFiles.AddRange(args);
            if (this.cancellation == null)
                this.Models_CancelAll();
            CancellationToken token = this.cancellation.Token;
            Task.Run(() => { this.DisplayModels(args, token); }, token);
        }
    }
}
