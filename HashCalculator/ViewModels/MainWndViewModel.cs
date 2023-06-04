﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class MainWndViewModel : BaseNotifiable
    {
        private readonly ModelStarter starter = new ModelStarter(8);
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private static readonly object serialNumberLock = new object();
        private delegate void ModelToTableDelegate(ModelArg arg);
        private readonly ModelToTableDelegate modelToTable;
        private int currentSerialNumber = 0;
        private int finishedNumberInQueue = 0;
        private int totalNumberInQueue = 0;
        private CancellationTokenSource cancellation;
        private List<ModelArg> droppedFiles = new List<ModelArg>();
        private string hashCheckReport = string.Empty;
        private QueueState queueState = QueueState.None;
        private static readonly object changeQueueCountLock = new object();
        private static readonly object concurrentLock = new object();
        private static readonly object displayModelLock = new object();
        private static readonly object displayModelTaskLock = new object();

        public MainWndViewModel()
        {
            Settings.Current.PropertyChanged += this.PropChangedAction;
            this.modelToTable = new ModelToTableDelegate(this.ModelToTable);
        }

        public ObservableCollection<HashViewModel> HashViewModels { get; }
            = new ObservableCollection<HashViewModel>();

        public string Report
        {
            get
            {
                if (string.IsNullOrEmpty(this.hashCheckReport))
                {
                    return "暂无校验报告...";
                }
                else
                {
                    return this.hashCheckReport;
                }
            }
            set
            {
                this.SetPropNotify(ref this.hashCheckReport, value);
            }
        }

        public QueueState State
        {
            get
            {
                return this.queueState;
            }
            set
            {
                if ((this.queueState == QueueState.None
                    || this.queueState == QueueState.Stopped)
                    && value == QueueState.Started)
                {
                    AppDispatcher.Invoke(() => { this.Report = string.Empty; });
                    this.SetPropNotify(ref this.queueState, value);
                }
                else if (this.queueState == QueueState.Started && value == QueueState.Stopped)
                {
                    this.GenerateVerificationReport();
                    this.SetPropNotify(ref this.queueState, value);
                }
            }
        }

        public int TotalNumberInQueue
        {
            get
            {
                return this.totalNumberInQueue;
            }
            set
            {
                this.SetPropNotify(ref this.totalNumberInQueue, value);
            }
        }

        public int FinishedInQueue
        {
            get
            {
                return this.finishedNumberInQueue;
            }
            set
            {
                this.SetPropNotify(ref this.finishedNumberInQueue, value);
            }
        }

        private void ModelToTable(ModelArg arg)
        {
            int modelSerial = this.SerialGet();
            HashViewModel model = new HashViewModel(modelSerial, arg);
            model.ComputeFinishedEvent += this.IncreaseQueueFinished;
            model.WaitingModelCanceledEvent += this.DecreaseQueueTotal;
            model.ModelCanbeStartedEvent += this.starter.PendingModel;
            model.StartupModel(false);
            this.HashViewModels.Add(model);
        }

        private void QueueItemCountChanged()
        {
#if DEBUG
            Console.WriteLine(
                $"已完成任务：{this.FinishedInQueue}，"
                + $"总数：{this.TotalNumberInQueue}");
#endif
            if (this.FinishedInQueue != this.TotalNumberInQueue
                && this.State != QueueState.Started)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.State = QueueState.Started;
                });
            }
            else if (this.FinishedInQueue == this.TotalNumberInQueue
                && this.State != QueueState.Stopped)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.FinishedInQueue = this.TotalNumberInQueue = 0;
                    this.State = QueueState.Stopped;
                });
            }
        }

        private void DisplayModels(IEnumerable<ModelArg> args, CancellationToken token)
        {
            int addedArgCount = 0;
            int argsCount = args.Count();
            if (argsCount == 0)
            {
                return;
            }
            AppDispatcher.Invoke(() =>
            {
                this.IncreaseQueueTotal(argsCount);
            });
            lock (displayModelLock)
            {
                foreach (ModelArg arg in args)
                {
                    if (token.IsCancellationRequested)
                    {
                        AppDispatcher.Invoke(() =>
                        {
                            this.DecreaseQueueTotal(argsCount - addedArgCount);
                        });
                        break;
                    }
                    AppDispatcher.Invoke(this.modelToTable, arg);
                    if (++addedArgCount % 1000 == 0)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
#if DEBUG
            Console.WriteLine($"已添加哈希模型：{addedArgCount}");
#endif
        }

        private void DecreaseQueueTotal(int number)
        {
            lock (changeQueueCountLock)
            {
                if (number < 0)
                {
                    number = 0;
                }
                this.TotalNumberInQueue -= number;
                this.QueueItemCountChanged();
            }
        }

        private void IncreaseQueueTotal(int number)
        {
            lock (changeQueueCountLock)
            {
                if (number < 0)
                {
                    number = 0;
                }
                this.TotalNumberInQueue += number;
                this.QueueItemCountChanged();
            }
        }

        private void IncreaseQueueFinished(int number)
        {
            lock (changeQueueCountLock)
            {
                if (number < 0)
                {
                    number = 0;
                }
                this.FinishedInQueue += number;
                this.QueueItemCountChanged();
            }
        }

        private int SerialGet()
        {
            lock (serialNumberLock) { return ++this.currentSerialNumber; }
        }

        private void SerialReset()
        {
            lock (serialNumberLock) { this.currentSerialNumber = 0; }
        }

        public void SetConcurrent(TaskNum num)
        {
            lock (concurrentLock)
            {
                switch (num)
                {
                    case TaskNum.One:
                        this.starter.Adjust(1);
                        break;
                    case TaskNum.Two:
                        this.starter.Adjust(2);
                        break;
                    case TaskNum.Four:
                        this.starter.Adjust(4);
                        break;
                    case TaskNum.Eight:
                        this.starter.Adjust(8);
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

        public void DisplayHashViewModelsTask(IEnumerable<ModelArg> args)
        {
            lock (displayModelTaskLock)
            {
                this.droppedFiles.AddRange(args);
                if (this.cancellation == null)
                {
                    this.cancellation = GlobalCancellation.Handle;
                }
                CancellationToken token = this.cancellation.Token;
                Task.Run(() => { this.DisplayModels(args, token); }, token);
            }
        }

        public void GenerateVerificationReport()
        {
            int noresult, unrelated, matched, mismatch,
                uncertain, succeeded, canceled, hasFailed;
            noresult = unrelated = matched = mismatch =
                uncertain = succeeded = canceled = hasFailed = 0;
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
                {
                    model.ShutdownModel();
                }
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
            {
                model.PauseOrContinueModel(PauseMode.Continue);
            }
        }

        public void Models_PauseAll()
        {
            foreach (var model in this.HashViewModels)
            {
                model.PauseOrContinueModel(PauseMode.Pause);
            }
        }

        public void Models_PauseOne(HashViewModel model)
        {
            model.PauseOrContinueModel(PauseMode.Invert);
        }

        public void Models_Restart(bool newLines, bool force)
        {
            if (!newLines)
            {
                int canbeStartModelCount = 0;
                foreach (var model in this.HashViewModels)
                {
                    if (model.StartupModel(force))
                    {
                        ++canbeStartModelCount;
                    }
                }
                this.IncreaseQueueTotal(canbeStartModelCount);
            }
            else
            {
                if (this.droppedFiles.Count <= 0)
                {
                    return;
                }
                List<ModelArg> args = this.droppedFiles;
                this.droppedFiles = new List<ModelArg>();
                this.DisplayHashViewModelsTask(args);
            }
        }

        public void Models_StartOne(HashViewModel viewModel)
        {
            this.IncreaseQueueTotal(1);
            viewModel.StartupModel(true);
        }

        private void PropChangedAction(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.SelectedTaskNumberLimit))
            {
                Task.Run(() =>
                {
                    this.SetConcurrent(Settings.Current.SelectedTaskNumberLimit);
                });
            }
        }

        public string StartCompareToolTip { get; } =
            "当面板为空时，如果校验依据选择的是通用格式的哈希值文本文件，则：\n" +
            "点击 [校验] 后程序会自动解析文件并在相同目录下寻找要计算哈希值的文件完成计算并显示校验结果。\n" +
            "通用格式的哈希值文件请参考程序 [导出结果] 功能导出的文件的内容排布格式。";
    }
}
