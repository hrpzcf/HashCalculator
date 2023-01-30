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
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private static readonly object serialNumberLock = new object();
        private static TaskList queueCleaners;
        private readonly DAddModelToTable addModelToTable;
        private readonly BlockingCollection<HashViewModel> queue
            = new BlockingCollection<HashViewModel>();
        private int currentSerialNumber = 0;
        private int finishedNumberInQueue = 0;
        private int totalNumberInQueue = 0;
        private CancellationTokenSource cancellation;
        private List<ModelArg> droppedFiles = new List<ModelArg>();
        private string hashCheckReport = string.Empty;
        private bool noDurationColumn;
        private bool noExportColumn;
        private QueueState queueState = QueueState.None;
        private static readonly object changeQueueCountLock = new object();
        private static readonly object checkQueueCountLock = new object();
        private static readonly object concurrentLock = new object();
        private static readonly object displayModelLock = new object();
        private static readonly object displayModelTaskLock = new object();
        private delegate void DAddModelToTable(ModelArg arg);

        public static AppViewModel Instance;
        public event PropertyChangedEventHandler PropertyChanged;

        public int FinishedInQueue
        {
            get
            {
                lock (changeQueueCountLock)
                    return this.finishedNumberInQueue;
            }
            set
            {
                lock (changeQueueCountLock)
                    this.finishedNumberInQueue = value;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<HashViewModel> HashViewModels { get; }
            = new ObservableCollection<HashViewModel>();

        public bool NoDurationColumn
        {
            get { return this.noDurationColumn; }
            set { this.noDurationColumn = value; this.OnPropertyChanged(); }
        }

        public bool NoExportColumn
        {
            get { return this.noExportColumn; }
            set { this.noExportColumn = value; this.OnPropertyChanged(); }
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

        public QueueState State
        {
            get => this.queueState;
            set
            {
                if ((this.queueState == QueueState.None
                    || this.queueState == QueueState.Stopped)
                    && value == QueueState.Started)
                {
                    AppDispatcher.Invoke(() => { this.Report = string.Empty; });
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
                lock (changeQueueCountLock)
                    return this.totalNumberInQueue;
            }
            set
            {
                lock (changeQueueCountLock)
                    this.totalNumberInQueue = value;
                this.OnPropertyChanged();
            }
        }

        public AppViewModel()
        {
            queueCleaners = new TaskList(8, this.HashModelMonitor);
            this.addModelToTable = new DAddModelToTable(this.AddModelToTable);
            Instance = this;
        }

        private void AddModelToTable(ModelArg arg)
        {
            int modelSerial = this.SerialGet();
            HashViewModel model = new HashViewModel(modelSerial, arg);
            model.ModelCanbeStartEvent += this.CanbeStartSubscriber;
            model.ComputeFinishedEvent += this.IncreaseQueueFinished;
            model.WaitingModelCanceledEvent += this.DecreaseQueueTotal;
            model.StartupModel(false);
            this.HashViewModels.Add(model);
        }

        private void AfterQueueItemsCountChanges()
        {
            lock (checkQueueCountLock)
            {
                if (this.TotalNumberInQueue > 0 && this.FinishedInQueue == 0 &&
                    this.State != QueueState.Started)
                {
                    AppDispatcher.Invoke(() => { this.State = QueueState.Started; });
                }
                else if (this.TotalNumberInQueue > 0 &&
                    this.FinishedInQueue == this.TotalNumberInQueue)
                {
                    AppDispatcher.Invoke(() =>
                    {
                        this.FinishedInQueue = this.TotalNumberInQueue = 0;
                        this.State = QueueState.Stopped;
                    });
                }
#if DEBUG
                else if (this.FinishedInQueue < 0 || this.TotalNumberInQueue < 0)
                {
                    Console.WriteLine(
                        $"[异常] 已完成任务：{this.FinishedInQueue}，总数：{this.TotalNumberInQueue}");
                    throw new InvalidOperationException("已完成数和总数异常");
                }
#endif
            }
        }

        private void CanbeStartSubscriber(HashViewModel model)
        {
            if (!this.queue.Contains(model))
            {
                this.queue.Add(model);
            }
#if DEBUG
            else
            {
                Console.WriteLine("当前队列已包含相同的元素");
            }
#endif
        }

        private void DecreaseQueueTotal(int number)
        {
            if (number < 0)
                number = 0;
            this.TotalNumberInQueue -= number;
            this.AfterQueueItemsCountChanges();
        }

        private void DisplayModels(IEnumerable<ModelArg> args, CancellationToken token)
        {
            int argsCount, remainingArgsCount;
            argsCount = remainingArgsCount = args.Count();
            if (argsCount == 0) return;
            AppDispatcher.Invoke(() => { this.IncreaseQueueTotal(argsCount); });
            lock (displayModelLock)
            {
                foreach (ModelArg arg in args)
                {
                    if (token.IsCancellationRequested)
                    {
                        AppDispatcher.Invoke(
                            () => { this.DecreaseQueueTotal(remainingArgsCount); });
                        break;
                    }
                    --remainingArgsCount;
                    AppDispatcher.Invoke(this.addModelToTable, arg);
                    Thread.Sleep(1);
                }
            }
#if DEBUG
            Console.WriteLine($"已添加哈希模型：{argsCount - remainingArgsCount}");
#endif
        }

        private void HashModelMonitor(object obj)
        {
            HashViewModel model;
            var token = (CancellationToken)obj;
            do
            {
                try { model = this.queue.Take(token); }
                catch (OperationCanceledException) { break; }
                model.ComputeHashValue();
            }
            while (!token.IsCancellationRequested);
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
            lock (serialNumberLock) { return ++this.currentSerialNumber; }
        }

        private void SerialReset()
        {
            lock (serialNumberLock) { this.currentSerialNumber = 0; }
        }

        public static void SetConcurrent(SimCalc num)
        {
            lock (concurrentLock)
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

        public void ClearHashViewModels()
        {
            this.droppedFiles.Clear();
            this.SerialReset();
            this.HashViewModels.Clear();
        }

        public void CreateTaskDisplayHashViewModels(IEnumerable<ModelArg> args)
        {
            lock (displayModelTaskLock)
            {
                this.droppedFiles.AddRange(args);
                if (this.cancellation == null)
                    this.cancellation = GlobalCancellation.Handle;
                CancellationToken token = this.cancellation.Token;
                Task.Run(() => { this.DisplayModels(args, token); }, token);
            }
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

        public void Models_CancelAll()
        {
            lock (displayModelTaskLock)
            {
                this.cancellation?.Cancel();
                foreach (var model in this.HashViewModels)
                    model.ShutdownModel();
                this.cancellation?.Dispose();
                this.cancellation = GlobalCancellation.Handle;
            }
        }

        public void Models_CancelOne(HashViewModel model)
        {
            model.ShutdownModel();
        }

        public void Models_ContinueAll()
        {
            foreach (var model in this.HashViewModels)
                model.PauseOrContinueModel(PauseMode.Continue);
        }

        public void Models_PauseAll()
        {
            foreach (var model in this.HashViewModels)
                model.PauseOrContinueModel(PauseMode.Pause);
        }

        public void Models_PauseOne(HashViewModel model)
        {
            model.PauseOrContinueModel(PauseMode.Invert);
        }

        public void Models_Restart(bool newLines)
        {
            if (!newLines)
            {
                bool force = !Settings.Current.RecalculateIncomplete;
                int canbeStartModelCount = 0;
                foreach (var model in this.HashViewModels)
                {
                    if (model.StartupModel(force))
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
                this.CreateTaskDisplayHashViewModels(args);
            }
        }

        public void Models_StartOne(HashViewModel viewModel)
        {
            this.IncreaseQueueTotal(1);
            viewModel.StartupModel(true);
        }

        public void SetColumnVisibility(bool noExportColumn, bool noDurationColumn)
        {
            this.NoExportColumn = noExportColumn;
            this.NoDurationColumn = noDurationColumn;
        }
    }
}
