using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace HashCalculator
{
    internal class MainWndViewModel : NotifiableModel
    {
        private readonly ModelStarter starter = new ModelStarter(8);
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private delegate void ModelToTableDelegate(ModelArg arg);
        private readonly ModelToTableDelegate modelToTable;
        private volatile int serial = 0;
        private int finishedNumberInQueue = 0;
        private int totalNumberInQueue = 0;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private List<ModelArg> displayedFiles = new List<ModelArg>();
        private string hashCheckReport = string.Empty;
        private QueueState queueState = QueueState.None;
        private readonly object changeQueueCountLock = new object();
        private readonly object changeTaskNumberLock = new object();
        private readonly object displayingModelLock = new object();
        private readonly object displayModelRequestLock = new object();

        public MainWndViewModel()
        {
            Settings.Current.PropertyChanged += this.PropChangedAction;
            this.modelToTable = new ModelToTableDelegate(this.ModelToTable);
        }

        public static ObservableCollection<HashViewModel> HashViewModels { get; }
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

        public ControlItem[] CopyModelsHashMenuItems { get; } =
        {
            new ControlItem("Base64 格式", new RelayCommand(CopyModelsHashBase64Action)),
            new ControlItem("十六进制大写", new RelayCommand(CopyModelsHashBinUpperAction)),
            new ControlItem("十六进制小写", new RelayCommand(CopyModelsHashBinLowerAction)),
        };

        public ICommand CopyModelsHashValueCmd { get; } =
            new RelayCommand(CopyModelsHashStringAction);

        public ICommand CopyFilesFullPathCmd { get; } =
            new RelayCommand(CopyFilesFullPathAction);

        public ICommand OpenFolderSelectItemsCmd { get; } =
            new RelayCommand(OpenFolderSelectItemsAction);

        public ICommand OpenModelsFilePathCmd { get; } =
            new RelayCommand(OpenModelsFilePathAction);

        public ICommand OpenFilesPropertyCmd { get; } =
            new RelayCommand(OpenFilesPropertyAction);

        public ICommand RefreshAllOutputTypeCmd { get; } =
            new RelayCommand(RefreshAllOutputTypeAction);

        public ICommand DeleteSelectedModelsFileCmd { get; } =
            new RelayCommand(DeleteSelectedModelsFileAction);

        public ICommand RemoveSelectedModelsCmd { get; } =
             new RelayCommand(RemoveSelectedModelsAction);

        private CancellationTokenSource Cancellation
        {
            get
            {
                return this._cancellation;
            }
            set
            {
                if (value is null)
                {
                    this._cancellation = new CancellationTokenSource();
                }
                else
                {
                    this._cancellation = value;
                }
            }
        }

        private static void CopyModelsHashBase64Action(object param)
        {
            CopyModelsHashValueAction(param, OutputType.BASE64);
        }

        private static void CopyModelsHashBinUpperAction(object param)
        {
            CopyModelsHashValueAction(param, OutputType.BinaryUpper);
        }

        private static void CopyModelsHashBinLowerAction(object param)
        {
            CopyModelsHashValueAction(param, OutputType.BinaryLower);
        }

        private static void CopyModelsHashStringAction(object param)
        {
            CopyModelsHashValueAction(param, OutputType.Unknown);
        }

        private static void CopyModelsHashValueAction(object param, OutputType output)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                if (count == 0)
                {
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < count; ++i)
                {
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    string formatedHashValue;
                    if (output != OutputType.Unknown)
                    {
                        formatedHashValue = (string)HashBytesOutputTypeCvt.Convert(model.Hash,
                            output);
                    }
                    else if (model.SelectedOutputType == OutputType.Unknown)
                    {
                        formatedHashValue = (string)HashBytesOutputTypeCvt.Convert(model.Hash,
                            Settings.Current.SelectedOutputType);
                    }
                    else
                    {
                        formatedHashValue = model.HashString;
                    }
                    if (i != 0)
                    {
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append(formatedHashValue);
                }
                if (stringBuilder.Length != 0)
                {
                    Clipboard.SetText(stringBuilder.ToString());
                }
            }
        }

        private static void CopyFilesFullPathAction(object param)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                if (count == 0)
                {
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < count; ++i)
                {
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    if (File.Exists(model.FileInfo.FullName))
                    {
                        if (i != 0)
                        {
                            stringBuilder.AppendLine();
                        }
                        stringBuilder.Append(model.FileInfo.FullName);
                    }
                }
                if (stringBuilder.Length != 0)
                {
                    Clipboard.SetText(stringBuilder.ToString());
                }
            }
        }

        private static void OpenFolderSelectItemsAction(object param)
        {
            if (param is IList selectedModels)
            {
                Dictionary<string, List<string>> groupByDir =
                    new Dictionary<string, List<string>>();
                int count = selectedModels.Count;
                for (int i = 0; i < count; ++i)
                {
                    bool isMatched = false;
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    foreach (string key in groupByDir.Keys)
                    {
                        if (model.FileInfo.ParentSameWith(key))
                        {
                            isMatched = true;
                            groupByDir[key].Add(model.FileInfo.Name);
                            break;
                        }
                    }
                    if (!isMatched)
                    {
                        groupByDir[model.FileInfo.DirectoryName] = new List<string> {
                            model.FileInfo.Name };
                    }
                }
                if (groupByDir.Any())
                {
                    foreach (string folderFullPath in groupByDir.Keys)
                    {
                        CommonUtils.OpenFolderAndSelectItems(
                            folderFullPath, groupByDir[folderFullPath]);
                    }
                }
            }
        }

        private static void OpenModelsFilePathAction(object param)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                for (int i = 0; i < count; ++i)
                {
                    var model = (HashViewModel)selectedModels[i];
                    if (!File.Exists(model.FileInfo.FullName))
                    {
                        continue;
                    }
                    NativeFunctions.ShellExecuteW(
                        MainWindow.WndHandle, "open",
                        model.FileInfo.FullName, null,
                        Path.GetDirectoryName(model.FileInfo.FullName),
                        ShowCmds.SW_SHOWNORMAL);
                }
            }
        }

        private static void OpenFilesPropertyAction(object param)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                for (int i = 0; i < count; ++i)
                {
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    if (!File.Exists(model.FileInfo.FullName))
                    {
                        continue;
                    }
                    var shellExecuteInfo = new SHELLEXECUTEINFOW();
                    shellExecuteInfo.cbSize = Marshal.SizeOf(shellExecuteInfo);
                    shellExecuteInfo.fMask = SEMaskFlags.SEE_MASK_INVOKEIDLIST;
                    shellExecuteInfo.hwnd = MainWindow.WndHandle;
                    shellExecuteInfo.lpVerb = "properties";
                    shellExecuteInfo.lpFile = model.FileInfo.FullName;
                    shellExecuteInfo.lpDirectory = model.FileInfo.DirectoryName;
                    shellExecuteInfo.nShow = ShowCmds.SW_SHOWNORMAL;
                    NativeFunctions.ShellExecuteExW(ref shellExecuteInfo);
                }
            }
        }

        private static void RefreshAllOutputTypeAction(object param)
        {
            foreach (HashViewModel model in HashViewModels)
            {
                if (model.HasBeenRun)
                {
                    model.SelectedOutputType = Settings.Current.SelectedOutputType;
                }
            }
        }

        private static void DeleteModelFileCallback(HashViewModel model)
        {
            try
            {
                model.FileInfo.Delete();
            }
            finally { }
        }

        public static void DeleteSelectedModelsFileAction(object param)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                if (MessageBox.Show(
                    MainWindow.This,
                    $"确定删除选中的 {count} 个文件吗？",
                    "提示",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Exclamation) != MessageBoxResult.OK)
                {
                    return;
                }
                HashViewModel[] models = selectedModels.Cast<HashViewModel>().ToArray();
                foreach (HashViewModel model in models)
                {
                    model.ModelShutdownEvent += DeleteModelFileCallback;
                    // 对 HashViewModels 的增删操作必定是在主线程上的，不用加锁
                    model.ShutdownModel();
                    HashViewModels.Remove(model);
                }
            }
        }

        private static void RemoveSelectedModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                HashViewModel[] models = selectedModels.Cast<HashViewModel>().ToArray();
                foreach (HashViewModel model in models)
                {
                    model.ShutdownModel();
                    // 对 HashViewModels 的增删操作必定是在主线程上的，不用加锁
                    HashViewModels.Remove(model);
                }
            }
        }

        private void ModelToTable(ModelArg arg)
        {
            HashViewModel model = new HashViewModel(
                Interlocked.Increment(ref this.serial), arg);
            model.ModelCapturedEvent += this.ModelCapturedAction;
            model.ModelCapturedEvent += this.starter.PendingModel;
            model.ModelReleasedEvent += this.ModelReleasedAction;
            HashViewModels.Add(model);
            model.StartupModel(false);
        }

        private void ModelCapturedAction(HashViewModel model)
        {
            lock (this.changeQueueCountLock)
            {
                ++this.TotalNumberInQueue;
                this.QueueItemCountChanged();
            }
        }

        private void ModelReleasedAction(HashViewModel model)
        {
            lock (this.changeQueueCountLock)
            {
                ++this.FinishedInQueue;
                this.QueueItemCountChanged();
            }
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

        public void ChangeTaskNumber(TaskNum num)
        {
            lock (this.changeTaskNumberLock)
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
            this.serial = 0;
            this.displayedFiles.Clear();
            HashViewModels.Clear();
        }

        public async void BeginDisplayModels(IEnumerable<ModelArg> args)
        {
            CancellationToken token;
            lock (this.displayModelRequestLock)
            {
                token = this.Cancellation.Token;
            }
            await Task.Run(() =>
            {
                lock (this.displayingModelLock)
                {
                    foreach (ModelArg arg in args)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        this.displayedFiles.Add(arg);
                        AppDispatcher.Invoke(this.modelToTable, arg);
                        Thread.Yield();
                    }
                }
            }, token);
        }

        public async void BeginDisplayModels(PathPackage package)
        {
            CancellationToken token;
            lock (this.displayModelRequestLock)
            {
                token = this.Cancellation.Token;
            }
            await Task.Run(() =>
            {
                lock (this.displayingModelLock)
                {
                    foreach (ModelArg arg in package)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        this.displayedFiles.Add(arg);
                        AppDispatcher.Invoke(this.modelToTable, arg);
                        Thread.Yield();
                    }

                }
            }, token);
        }

        public void GenerateVerificationReport()
        {
            int noresult, unrelated, matched, mismatch,
                uncertain, succeeded, canceled, hasFailed;
            noresult = unrelated = matched = mismatch =
                uncertain = succeeded = canceled = hasFailed = 0;
            foreach (HashViewModel hm in HashViewModels)
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
                    case HashResult.Canceled:
                        ++canceled;
                        break;
                    case HashResult.Failed:
                        ++hasFailed;
                        break;
                    case HashResult.Succeeded:
                        ++succeeded;
                        break;
                }
            }
            this.Report
                = $"总项数：{HashViewModels.Count}\n已成功：{succeeded}\n"
                + $"已失败：{hasFailed}\n已取消：{canceled}\n\n"
                + $"校验汇总：\n已匹配：{matched}\n不匹配：{mismatch}\n"
                + $"不确定：{uncertain}\n无关联：{unrelated}\n未校验：{noresult}";
        }

        public void Models_CancelAll()
        {
            lock (this.displayModelRequestLock)
            {
                this.Cancellation?.Cancel();
                foreach (HashViewModel model in HashViewModels)
                {
                    model.ShutdownModel();
                }
                this.Cancellation?.Dispose();
                this.Cancellation = new CancellationTokenSource();
            }
        }

        public void Models_CancelOne(HashViewModel model)
        {
            model.ShutdownModel();
        }

        public void Models_ContinueAll()
        {
            foreach (HashViewModel model in HashViewModels)
            {
                model.PauseOrContinueModel(PauseMode.Continue);
            }
        }

        public void Models_PauseAll()
        {
            foreach (HashViewModel model in HashViewModels)
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
                foreach (HashViewModel model in HashViewModels)
                {
                    model.StartupModel(force);
                }
            }
            else
            {
                if (this.displayedFiles.Count <= 0)
                {
                    return;
                }
                List<ModelArg> args = this.displayedFiles;
                this.displayedFiles = new List<ModelArg>();
                this.BeginDisplayModels(args);
            }
        }

        public void Models_StartOne(HashViewModel viewModel)
        {
            viewModel.StartupModel(false);
        }

        private void PropChangedAction(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.SelectedTaskNumberLimit))
            {
                Task.Run(() =>
                {
                    this.ChangeTaskNumber(Settings.Current.SelectedTaskNumberLimit);
                });
            }
        }

        public string StartCompareToolTip { get; } =
            "当面板为空时，如果校验依据选择的是通用格式的哈希值文本文件，则：\n" +
            "点击 [校验] 后程序会自动解析文件并在相同目录下寻找要计算哈希值的文件完成计算并显示校验结果。\n" +
            "通用格式的哈希值文件请参考程序 [导出结果] 功能导出的文件的内容排布格式。";
    }
}
