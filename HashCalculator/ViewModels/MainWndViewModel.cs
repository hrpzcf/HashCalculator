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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator
{
    internal class MainWndViewModel : NotifiableModel
    {
        public const string formatForCopyHash = "{0}\n";
        private const string formatForExportHash = "#{0} *{1} *{2}\n";
        private readonly HashChecklist MainChecklist = new HashChecklist();
        private readonly ModelStarter starter =
            new ModelStarter((int)Settings.Current.SelectedTaskNumberLimit, 8);
        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private delegate void AddModelDelegate(ModelArg arg);
        private readonly AddModelDelegate addModelAction;
        private volatile int serial = 0;
        private int tobeComputedModelsCount = 0;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private CancellationTokenSource searchCancellation = new CancellationTokenSource();
        private List<ModelArg> displayedFiles = new List<ModelArg>();
        private string hashCheckReport = string.Empty;
        private string hashValueStringOrChecklistPath = null;
        private RunningState queueState = RunningState.None;
        private FilterAndCmdPanel commandPanelInst = null;
        private readonly object displayingModelLock = new object();
        private readonly object displayModelRequestLock = new object();
        private readonly object tobeComputedModelsCountLock = new object();
        private RelayCommand openCommandPanelCmd;
        private RelayCommand openSelectAlgoWndCmd;
        private RelayCommand mainWindowTopmostCmd;
        private RelayCommand clearAllTableLinesCmd;
        private RelayCommand exportHashResultCmd;
        private RelayCommand copyAndRestartModelsCmd;
        private RelayCommand refreshOriginalModelsCmd;
        private RelayCommand forceRefreshOriginalModelsCmd;
        private RelayCommand selectChecklistFileCmd;
        private RelayCommand startVerifyHashValueCmd;
        private RelayCommand openSettingsPanelCmd;
        private RelayCommand openAboutWindowCmd;
        private RelayCommand selectFilesToHashCmd;
        private RelayCommand selectFolderToHashCmd;
        private RelayCommand cancelDisplayedModelsCmd;
        private RelayCommand pauseDisplayedModelsCmd;
        private RelayCommand continueDisplayedModelsCmd;
        private RelayCommand copyFilesNameCmd;
        private RelayCommand copyFilesFullPathCmd;
        private RelayCommand openFolderSelectItemsCmd;
        private RelayCommand openModelsFilePathCmd;
        private RelayCommand openFilesPropertyCmd;
        private RelayCommand refreshAllOutputTypeCmd;
        private RelayCommand deleteSelectedModelsFileCmd;
        private RelayCommand removeSelectedModelsCmd;
        private RelayCommand stopEnumeratingPackageCmd;
        private GenericItemModel[] copyModelsHashMenuCmds;
        private GenericItemModel[] copyModelsAllHashesMenuCmds;
        private GenericItemModel[] ctrlHashViewModelTaskCmds;
        private GenericItemModel[] switchDisplayedAlgoCmds;

        public MainWndViewModel()
        {
            CurrentModel = this;
            HashViewModelsViewSrc = new CollectionViewSource();
            HashViewModelsViewSrc.Source = HashViewModels;
            HashViewModelsView = HashViewModelsViewSrc.View;
            Settings.Current.PropertyChanged += this.SettingsPropChangedAction;
            this.addModelAction = new AddModelDelegate(this.AddModelAction);
        }

        public static MainWndViewModel CurrentModel
        {
            get;
            private set;
        }

        public Window OwnerWnd { get; set; }

        /// <summary>
        /// 使用此属性相当于 HashViewModelsViewSrc.View 的简写
        /// </summary>
        public static ICollectionView HashViewModelsView
        {
            get;
            private set;
        }

        /// <summary>
        /// 用于在 .xaml 文件内绑定到 DataGrid 的 ItemsSource 属性
        /// 因为直接用 HashViewModels 属性绑定到 ItemsSource，则对视图的分组等操作不会生效
        /// </summary>
        public static CollectionViewSource HashViewModelsViewSrc
        {
            get;
            private set;
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

        public RunningState State
        {
            get
            {
                return this.queueState;
            }
            set
            {
                if ((this.queueState == RunningState.None
                    || this.queueState == RunningState.Stopped)
                    && value == RunningState.Started)
                {
                    this.Report = string.Empty;
                    this.SetPropNotify(ref this.queueState, value);
                }
                else if (this.queueState == RunningState.Started && value == RunningState.Stopped)
                {
                    this.GenerateFileHashCheckReport();
                    this.SetPropNotify(ref this.queueState, value);
                }
            }
        }

        public int TobeComputedModelsCount
        {
            get
            {
                return this.tobeComputedModelsCount;
            }
            set
            {
                this.SetPropNotify(ref this.tobeComputedModelsCount, value);
            }
        }

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

        public void SetHashStringOrChecklistPath()
        {
            try
            {
                if (CommonUtils.ClipboardGetText(out string clipboardText))
                {
                    if (Settings.Current.MinCharsNumRequiredForMonitoringClipboard
                        > Settings.Current.MaxCharsNumRequiredForMonitoringClipboard)
                    {
                        CommonUtils.Swap(
                            ref Settings.Current.minCharsNumRequiredForMonitoringClipboard,
                            ref Settings.Current.maxCharsNumRequiredForMonitoringClipboard);
                    }
                    if (clipboardText.Length < Settings.Current.MinCharsNumRequiredForMonitoringClipboard
                        || clipboardText.Length > Settings.Current.MaxCharsNumRequiredForMonitoringClipboard)
                    {
                        return;
                    }
                    if (CommonUtils.IsValidHashBytesString(clipboardText))
                    {
                        this.HashStringOrChecklistPath = clipboardText;
                        if (this.State != RunningState.Started)
                        {
                            this.CheckFileHashUseInfoFromHashStringOrChecklistPath();
                            if (Settings.Current.SwitchMainWndFgWhenNewHashCopied)
                            {
                                CommonUtils.ShowWindowForeground(MainWindow.ProcessId);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void ModelCapturedAction(HashViewModel model)
        {
            lock (this.tobeComputedModelsCountLock)
            {
                if (this.TobeComputedModelsCount++ == 0)
                {
                    synchronization.Invoke(() => { this.State = RunningState.Started; });
                }
            }
        }

        private void ModelReleasedAction(HashViewModel model)
        {
            lock (this.tobeComputedModelsCountLock)
            {
                if (--this.TobeComputedModelsCount == 0)
                {
                    synchronization.Invoke(() => { this.State = RunningState.Stopped; });
                }
            }
        }

        private void AddModelAction(ModelArg arg)
        {
            HashViewModel model = new HashViewModel(
                Interlocked.Increment(ref this.serial), arg);
            model.ModelCapturedEvent += this.ModelCapturedAction;
            model.ModelCapturedEvent += this.starter.PendingModel;
            model.ModelReleasedEvent += this.ModelReleasedAction;
            model.StartupModel(false);
            HashViewModels.Add(model);
        }

        public async void BeginDisplayModels(PathPackage package)
        {
            CancellationToken token;
            lock (this.displayModelRequestLock)
            {
                token = this.Cancellation.Token;
            }
            package.StopSearchingToken = this.searchCancellation.Token;
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
                        synchronization.Invoke(this.addModelAction, DispatcherPriority.Background, arg);
                        Thread.Yield();
                    }
                }
            }, token);
        }

        public async void BeginDisplayModels(IEnumerable<ModelArg> args, bool reset)
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
                        if (reset)
                        {
                            arg.PresetAlgos = null;
                        }
                        this.displayedFiles.Add(arg);
                        synchronization.Invoke(this.addModelAction, DispatcherPriority.Background, arg);
                        Thread.Yield();
                    }
                }
            }, token);
        }

        public void GenerateFileHashCheckReport()
        {
            int noresult, unrelated, matched, mismatch,
                uncertain, succeeded, canceled, hasFailed, totalHash;
            noresult = unrelated = matched = mismatch =
                uncertain = succeeded = canceled = hasFailed = totalHash = 0;
            foreach (HashViewModel hm in HashViewModels)
            {
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
                if (hm.AlgoInOutModels != null)
                {
                    foreach (AlgoInOutModel model in hm.AlgoInOutModels)
                    {
                        ++totalHash;
                        switch (model.HashCmpResult)
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
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append($"总行数：{HashViewModels.Count}\n已成功：{succeeded}\n");
            sb.Append($"已失败：{hasFailed}\n已取消：{canceled}\n\n");
            sb.Append($"校验汇总：\n算法数：{totalHash}\n");
            sb.Append($"已匹配：{matched}\n不匹配：{mismatch}\n");
            sb.Append($"不确定：{uncertain}\n无关联：{unrelated}\n未校验：{noresult}");
            this.Report = sb.ToString();
        }

        /// <summary>
        /// 需要立即响应的设置变更
        /// </summary>
        private void SettingsPropChangedAction(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.SelectedTaskNumberLimit))
            {
                this.starter.BeginAdjust((int)Settings.Current.SelectedTaskNumberLimit);
            }
            else if (e.PropertyName == nameof(Settings.Current.RunInMultiInstMode))
            {
                MappedFiler.RunMultiMode = Settings.Current.RunInMultiInstMode;
            }
        }

        private void OpenCommandPanelAction(object param)
        {
            if (this.commandPanelInst != null)
            {
                if (!this.commandPanelInst.CheckPanelPosition())
                {
                    this.commandPanelInst.Close();
                }
            }
            else
            {
                this.commandPanelInst = new FilterAndCmdPanel((o, e) => { this.commandPanelInst = null; });
                this.commandPanelInst.Owner = MainWindow.This;
                this.commandPanelInst.Show();
            }
        }

        public ICommand OpenCommandPanelCmd
        {
            get
            {
                if (this.openCommandPanelCmd == null)
                {
                    this.openCommandPanelCmd = new RelayCommand(this.OpenCommandPanelAction);
                }
                return this.openCommandPanelCmd;
            }
        }

        private void OpenSelectAlgoWndAction(object param)
        {
            new AlgosPanel() { Owner = MainWindow.This }.ShowDialog();
        }

        public ICommand OpenSelectAlgoWndCmd
        {
            get
            {
                if (this.openSelectAlgoWndCmd == null)
                {
                    this.openSelectAlgoWndCmd = new RelayCommand(this.OpenSelectAlgoWndAction);
                }
                return this.openSelectAlgoWndCmd;
            }
        }

        private void CopyModelsHashValueAction(object param, OutputType output)
        {
            if (param is IList selectedModels && selectedModels.AnyItem())
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (HashViewModel model in selectedModels)
                {
                    string formatedHashString;
                    if (output != OutputType.Unknown)
                    {
                        formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                            model.CurrentInOutModel.HashResult, output);
                    }
                    else if (model.SelectedOutputType != OutputType.Unknown)
                    {
                        formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                            model.CurrentInOutModel.HashResult, model.SelectedOutputType);
                    }
                    else
                    {
                        formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                            model.CurrentInOutModel.HashResult, Settings.Current.SelectedOutputType);
                    }
                    stringBuilder.AppendFormat(formatForCopyHash, formatedHashString);
                }
                if (stringBuilder.Length > 0)
                {
                    CommonUtils.ClipboardSetText(stringBuilder.ToString());
                }
            }
        }

        private void CopyModelsHashBase64Action(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BASE64);
        }

        private void CopyModelsHashBinUpperAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryUpper);
        }

        private void CopyModelsHashBinLowerAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryLower);
        }

        public GenericItemModel[] CopyModelsHashMenuCmds
        {
            get
            {
                if (this.copyModelsHashMenuCmds is null)
                {
                    this.copyModelsHashMenuCmds = new GenericItemModel[]
                    {
                        new GenericItemModel("Base64 格式", new RelayCommand(this.CopyModelsHashBase64Action)),
                        new GenericItemModel("十六进制大写", new RelayCommand(this.CopyModelsHashBinUpperAction)),
                        new GenericItemModel("十六进制小写", new RelayCommand(this.CopyModelsHashBinLowerAction)),
                    };
                }
                return this.copyModelsHashMenuCmds;
            }
        }

        private void CopyModelsAllHashesAction(object param, OutputType output)
        {
            if (param is IList selectedModels && selectedModels.AnyItem())
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (HashViewModel model in selectedModels)
                {
                    foreach (AlgoInOutModel algoInOutModel in model.AlgoInOutModels)
                    {
                        string formatedHashString;
                        if (output != OutputType.Unknown)
                        {
                            formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                                algoInOutModel.HashResult, output);
                        }
                        else if (model.SelectedOutputType != OutputType.Unknown)
                        {
                            formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                                algoInOutModel.HashResult, model.SelectedOutputType);
                        }
                        else
                        {
                            formatedHashString = BytesToStrByOutputTypeCvt.Convert(
                                algoInOutModel.HashResult, Settings.Current.SelectedOutputType);
                        }
                        stringBuilder.AppendFormat(formatForCopyHash, formatedHashString);
                    }
                }
                if (stringBuilder.Length > 0)
                {
                    CommonUtils.ClipboardSetText(stringBuilder.ToString());
                }
            }
        }

        private void CopyModelsAllBase64HashesAction(object param)
        {
            this.CopyModelsAllHashesAction(param, OutputType.BASE64);
        }

        private void CopyModelsAllBinUpperHashesAction(object param)
        {
            this.CopyModelsAllHashesAction(param, OutputType.BinaryUpper);
        }

        private void CopyModelsAllBinLowerHashesAction(object param)
        {
            this.CopyModelsAllHashesAction(param, OutputType.BinaryLower);
        }

        public GenericItemModel[] CopyModelsAllHashesMenuCmds
        {
            get
            {
                if (this.copyModelsAllHashesMenuCmds == null)
                {
                    this.copyModelsAllHashesMenuCmds = new GenericItemModel[]
                    {
                        new GenericItemModel("Base64 格式", new RelayCommand(this.CopyModelsAllBase64HashesAction)),
                        new GenericItemModel("十六进制大写", new RelayCommand(this.CopyModelsAllBinUpperHashesAction)),
                        new GenericItemModel("十六进制小写", new RelayCommand(this.CopyModelsAllBinLowerHashesAction)),
                    };
                }
                return this.copyModelsAllHashesMenuCmds;
            }
        }

        private void CopyFilesNameOrPathAction(object param, bool copyName)
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
                        if (copyName)
                        {
                            stringBuilder.Append(model.FileInfo.Name);
                        }
                        else
                        {
                            stringBuilder.Append(model.FileInfo.FullName);
                        }
                    }
                }
                if (stringBuilder.Length != 0)
                {
                    CommonUtils.ClipboardSetText(stringBuilder.ToString());
                }
            }
        }

        private void CopyFilesNameAction(object param)
        {
            this.CopyFilesNameOrPathAction(param, true);
        }

        public ICommand CopyFilesNameCmd
        {
            get
            {
                if (this.copyFilesNameCmd is null)
                {
                    this.copyFilesNameCmd = new RelayCommand(this.CopyFilesNameAction);
                }
                return this.copyFilesNameCmd;
            }
        }

        private void CopyFilesPathAction(object param)
        {
            this.CopyFilesNameOrPathAction(param, false);
        }

        public ICommand CopyFilesFullPathCmd
        {
            get
            {
                if (this.copyFilesFullPathCmd is null)
                {
                    this.copyFilesFullPathCmd = new RelayCommand(this.CopyFilesPathAction);
                }
                return this.copyFilesFullPathCmd;
            }
        }

        private void OpenFolderSelectItemsAction(object param)
        {
            if (param is IList selectedModels && selectedModels.AnyItem())
            {
                Dictionary<string, List<string>> groupByDir =
                    new Dictionary<string, List<string>>();
                foreach (HashViewModel model in selectedModels)
                {
                    bool isMatched = false;
                    if (!Path.IsPathRooted(model.ModelArg.FilePath))
                    {
                        continue;
                    }
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

        public ICommand OpenFolderSelectItemsCmd
        {
            get
            {
                if (this.openFolderSelectItemsCmd is null)
                {
                    this.openFolderSelectItemsCmd = new RelayCommand(this.OpenFolderSelectItemsAction);
                }
                return this.openFolderSelectItemsCmd;
            }
        }

        private void OpenModelsFilePathAction(object param)
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
                        ShowCmd.SW_SHOWNORMAL);
                }
            }
        }

        public ICommand OpenModelsFilePathCmd
        {
            get
            {
                if (this.openModelsFilePathCmd is null)
                {
                    this.openModelsFilePathCmd = new RelayCommand(this.OpenModelsFilePathAction);
                }
                return this.openModelsFilePathCmd;
            }
        }

        private void OpenFilesPropertyAction(object param)
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
                    shellExecuteInfo.nShow = ShowCmd.SW_SHOWNORMAL;
                    NativeFunctions.ShellExecuteExW(ref shellExecuteInfo);
                }
            }
        }

        public ICommand OpenFilesPropertyCmd
        {
            get
            {
                if (this.openFilesPropertyCmd is null)
                {
                    this.openFilesPropertyCmd = new RelayCommand(this.OpenFilesPropertyAction);
                }
                return this.openFilesPropertyCmd;
            }
        }

        private void RefreshAllOutputTypeAction(object param)
        {
            foreach (HashViewModel model in HashViewModels)
            {
                if (model.HasBeenRun)
                {
                    model.SelectedOutputType = Settings.Current.SelectedOutputType;
                }
            }
        }

        public ICommand RefreshAllOutputTypeCmd
        {
            get
            {
                if (this.refreshAllOutputTypeCmd is null)
                {
                    this.refreshAllOutputTypeCmd = new RelayCommand(this.RefreshAllOutputTypeAction);
                }
                return this.refreshAllOutputTypeCmd;
            }
        }

        private void DeleteModelFileAction(HashViewModel model)
        {
            if (Settings.Current.PermanentlyDeleteFiles)
            {
                try
                {
                    model.FileInfo.Delete();
                }
                catch (Exception)
                {
                    MessageBox.Show(this.OwnerWnd,
                        $"文件删除失败：\n{model.FileInfo.FullName}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (!CommonUtils.SendToRecycleBin(
                        MainWindow.WndHandle, model.FileInfo.FullName))
                {
                    MessageBox.Show(this.OwnerWnd,
                        $"文件移动到回收站失败：\n{model.FileInfo.FullName}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteSelectedModelsFileAction(object param)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                if (count == 0)
                {
                    return;
                }
                string deleteFileTip;
                if (Settings.Current.PermanentlyDeleteFiles)
                {
                    deleteFileTip = $"确定永久删除选中的 {count} 个文件吗？";
                }
                else
                {
                    deleteFileTip = $"确定把选中的 {count} 个文件移动到回收站吗？";
                }
                if (MessageBox.Show(
                    this.OwnerWnd, deleteFileTip, "提示",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Exclamation,
                    MessageBoxResult.Cancel) != MessageBoxResult.OK)
                {
                    return;
                }
                HashViewModel[] models = selectedModels.Cast<HashViewModel>().ToArray();
                foreach (HashViewModel model in models)
                {
                    model.ModelShutdownEvent += this.DeleteModelFileAction;
                    // 对 HashViewModels 的增删操作是在主线程上进行的，不用加锁
                    model.ShutdownModel();
                    HashViewModels.Remove(model);
                }
                this.GenerateFileHashCheckReport();
            }
        }

        public ICommand DeleteSelectedModelsFileCmd
        {
            get
            {
                if (this.deleteSelectedModelsFileCmd is null)
                {
                    this.deleteSelectedModelsFileCmd = new RelayCommand(this.DeleteSelectedModelsFileAction);
                }
                return this.deleteSelectedModelsFileCmd;
            }
        }

        private void RemoveSelectedModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                HashViewModel[] models = selectedModels.Cast<HashViewModel>().ToArray();
                foreach (HashViewModel model in models)
                {
                    model.ShutdownModel();
                    // 对 HashViewModels 的增删操作是在主线程上进行的，不用加锁
                    HashViewModels.Remove(model);
                }
                this.GenerateFileHashCheckReport();
            }
        }

        public ICommand RemoveSelectedModelsCmd
        {
            get
            {
                if (this.removeSelectedModelsCmd is null)
                {
                    this.removeSelectedModelsCmd = new RelayCommand(this.RemoveSelectedModelsAction);
                }
                return this.removeSelectedModelsCmd;
            }
        }

        private void MainWindowTopmostAction(object param)
        {
            Settings.Current.MainWndTopmost = !Settings.Current.MainWndTopmost;
        }

        public ICommand MainWindowTopmostCmd
        {
            get
            {
                if (this.mainWindowTopmostCmd is null)
                {
                    this.mainWindowTopmostCmd = new RelayCommand(this.MainWindowTopmostAction);
                }
                return this.mainWindowTopmostCmd;
            }
        }

        private void ClearAllTableLinesAction(object param)
        {
            this.CancelDisplayedModelsAction(null);
            this.serial = 0;
            this.displayedFiles.Clear();
            HashViewModels.Clear();
        }

        public ICommand ClearAllTableLinesCmd
        {
            get
            {
                if (this.clearAllTableLinesCmd is null)
                {
                    this.clearAllTableLinesCmd = new RelayCommand(this.ClearAllTableLinesAction);
                }
                return this.clearAllTableLinesCmd;
            }
        }

        private void ExporHashResultAction(object param)
        {
            if (!HashViewModels.Any())
            {
                MessageBox.Show(this.OwnerWnd, "列表中没有任何可以导出的条目。", "提示");
                return;
            }
            string fileName = "hashsums.txt";
            string filter = "文本文件|*.txt|哈希值校验依据|*.hcb|所有文件|*.*";
            switch (Settings.Current.ResultFileTypeExportAs)
            {
                default:
                case ExportType.TxtFile:
                    break;
                case ExportType.HcbFile:
                    fileName = "hashsums.hcb";
                    filter = "哈希值校验依据|*.hcb|文本文件|*.txt|所有文件|*.*";
                    break;
            }
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = filter,
                FileName = fileName,
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (sf.ShowDialog() != true)
            {
                return;
            }
            Settings.Current.LastUsedPath = Path.GetDirectoryName(sf.FileName);
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (Settings.Current.HowToExportHashValues == ExportAlgos.AllCalculated)
                {
                    foreach (HashViewModel hm in HashViewModels)
                    {
                        if (hm.Result != HashResult.Succeeded)
                        {
                            continue;
                        }
                        foreach (AlgoInOutModel model in hm.AlgoInOutModels)
                        {
                            if (!model.Export)
                            {
                                continue;
                            }
                            string hash = BytesToStrByOutputTypeCvt.Convert(model.HashResult,
                                Settings.Current.SelectedOutputType);
                            stringBuilder.AppendFormat(formatForExportHash, model.AlgoName, hash,
                                hm.FileName);
                        }
                    }
                }
                else if (Settings.Current.HowToExportHashValues == ExportAlgos.Current)
                {
                    foreach (HashViewModel hm in HashViewModels)
                    {
                        if (hm.Result == HashResult.Succeeded && hm.CurrentInOutModel.Export)
                        {
                            if (string.IsNullOrEmpty(hm.CurrentHashString))
                            {
                                string hash = BytesToStrByOutputTypeCvt.Convert(hm.CurrentInOutModel.HashResult,
                                    Settings.Current.SelectedOutputType);
                                stringBuilder.AppendFormat(formatForExportHash, hm.CurrentInOutModel.AlgoName, hash,
                                    hm.FileName);
                            }
                            else
                            {
                                stringBuilder.AppendFormat(formatForExportHash, hm.CurrentInOutModel.AlgoName,
                                    hm.CurrentHashString, hm.FileName);
                            }
                        }
                    }
                }
                if (stringBuilder.Length > 0)
                {
                    File.WriteAllText(sf.FileName, stringBuilder.ToString());
                }
                else
                {
                    MessageBox.Show(this.OwnerWnd, $"收集到的哈希结果为空", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.OwnerWnd, $"哈希值导出失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public ICommand ExportHashResultCmd
        {
            get
            {
                if (this.exportHashResultCmd is null)
                {
                    this.exportHashResultCmd = new RelayCommand(this.ExporHashResultAction);
                }
                return this.exportHashResultCmd;
            }
        }

        public void StartModels(bool newLines, bool force)
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
                if (this.displayedFiles.Any())
                {
                    List<ModelArg> args = this.displayedFiles;
                    this.displayedFiles = new List<ModelArg>();
                    this.BeginDisplayModels(args, true);
                }
            }
        }

        private void CopyAndRestartModelsAction(object param)
        {
            this.StartModels(newLines: true, force: false);
        }

        public ICommand CopyAndRestartModelsCmd
        {
            get
            {
                if (this.copyAndRestartModelsCmd is null)
                {
                    this.copyAndRestartModelsCmd = new RelayCommand(this.CopyAndRestartModelsAction);
                }
                return this.copyAndRestartModelsCmd;
            }
        }

        private void RefreshOriginalModelsAction(object param)
        {
            this.StartModels(newLines: false, force: false);
        }

        public ICommand RefreshOriginalModelsCmd
        {
            get
            {
                if (this.refreshOriginalModelsCmd is null)
                {
                    this.refreshOriginalModelsCmd = new RelayCommand(this.RefreshOriginalModelsAction);
                }
                return this.refreshOriginalModelsCmd;
            }
        }

        private void ForceRefreshOriginalModelsAction(object param)
        {
            this.StartModels(newLines: false, force: true);
        }

        public ICommand ForceRefreshOriginalModelsCmd
        {
            get
            {
                if (this.forceRefreshOriginalModelsCmd is null)
                {
                    this.forceRefreshOriginalModelsCmd = new RelayCommand(this.ForceRefreshOriginalModelsAction);
                }
                return this.forceRefreshOriginalModelsCmd;
            }
        }

        private void GenerateOriginFileHashCheckReport()
        {
            foreach (HashViewModel hm in HashViewModels)
            {
                if (hm.AlgoInOutModels != null)
                {
                    foreach (AlgoInOutModel model in hm.AlgoInOutModels)
                    {
                        model.HashCmpResult = CmpRes.NoResult;
                    }
                }
            }
            this.GenerateFileHashCheckReport();
        }

        public void CheckFileHashUseInfoFromHashStringOrChecklistPath()
        {
            if (string.IsNullOrEmpty(this.HashStringOrChecklistPath))
            {
                this.GenerateOriginFileHashCheckReport();
                return;
            }
            string reasonForFailure;
            // HashStringOrChecklistPath 不是一个文件
            if (!File.Exists(this.HashStringOrChecklistPath))
            {
                reasonForFailure = this.MainChecklist.UpdateWithText(this.HashStringOrChecklistPath);
            }
            // HashStringOrChecklistPath 是一个文件，但哈希结果列表不是空
            else if (HashViewModels.Any())
            {
                reasonForFailure = this.MainChecklist.UpdateWithFile(this.HashStringOrChecklistPath);
            }
            // HashStringOrChecklistPath 是一个文件，且哈希结果列表也是空
            else
            {
                HashChecklist newChecklist = new HashChecklist(this.HashStringOrChecklistPath);
                if (newChecklist.ReasonForFailure != null)
                {
                    MessageBox.Show(MainWindow.This, newChecklist.ReasonForFailure, "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    this.BeginDisplayModels(new PathPackage(Path.GetDirectoryName(this.HashStringOrChecklistPath),
                        Settings.Current.SelectedQVSPolicy, newChecklist));
                }
                return;
            }
            if (reasonForFailure == null)
            {
                foreach (HashViewModel hm in HashViewModels)
                {
                    hm.SetHashCheckResultForModel(this.MainChecklist);
                }
                this.GenerateFileHashCheckReport();
            }
            else
            {
                this.GenerateOriginFileHashCheckReport();
                MessageBox.Show(MainWindow.This, reasonForFailure, "错误", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SelectChecklistFileAction(object param)
        {
            CommonOpenFileDialog openFile = new CommonOpenFileDialog
            {
                Title = "选择【文件哈希值清单】文件",
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (openFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Current.LastUsedPath = Path.GetDirectoryName(openFile.FileName);
                this.HashStringOrChecklistPath = openFile.FileName;
            }
        }

        public ICommand SelectChecklistFileCmd
        {
            get
            {
                if (this.selectChecklistFileCmd == null)
                {
                    this.selectChecklistFileCmd = new RelayCommand(this.SelectChecklistFileAction);
                }
                return this.selectChecklistFileCmd;
            }
        }

        private void CheckFileHashValueAction(object param)
        {
            this.CheckFileHashUseInfoFromHashStringOrChecklistPath();
        }

        public ICommand StartVerifyHashValueCmd
        {
            get
            {
                if (this.startVerifyHashValueCmd is null)
                {
                    this.startVerifyHashValueCmd = new RelayCommand(this.CheckFileHashValueAction);
                }
                return this.startVerifyHashValueCmd;
            }
        }

        private void OpenSettingsPanelAction(object param)
        {
            new SettingsPanel() { Owner = this.OwnerWnd }.ShowDialog();
        }

        public ICommand OpenSettingsPanelCmd
        {
            get
            {
                if (this.openSettingsPanelCmd is null)
                {
                    this.openSettingsPanelCmd = new RelayCommand(this.OpenSettingsPanelAction);
                }
                return this.openSettingsPanelCmd;
            }
        }

        private void OpenAboutWindowAction(object param)
        {
            new AboutWindow() { Owner = MainWindow.This }.ShowDialog();
        }

        public ICommand OpenAboutWindowCmd
        {
            get
            {
                if (this.openAboutWindowCmd is null)
                {
                    this.openAboutWindowCmd = new RelayCommand(this.OpenAboutWindowAction);
                }
                return this.openAboutWindowCmd;
            }
        }

        private void SelectFilesToHashAction(object param)
        {
            CommonOpenFileDialog fileOpen = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = Settings.Current.LastUsedPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (fileOpen.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Settings.Current.LastUsedPath =
                    Path.GetDirectoryName(fileOpen.FileNames.ElementAt(0));
            this.BeginDisplayModels(
                new PathPackage(fileOpen.FileNames, Settings.Current.SelectedSearchPolicy));
        }

        public ICommand SelectFilesToHashCmd
        {
            get
            {
                if (this.selectFilesToHashCmd is null)
                {
                    this.selectFilesToHashCmd = new RelayCommand(this.SelectFilesToHashAction);
                }
                return this.selectFilesToHashCmd;
            }
        }

        private void SelectFolderToHashAction(object param)
        {
            SearchPolicy policy = Settings.Current.SelectedSearchPolicy;
            if (policy == SearchPolicy.DontSearch)
            {
                policy = SearchPolicy.Children;
            }
            CommonOpenFileDialog folderOpen = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = Settings.Current.LastUsedPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (folderOpen.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Settings.Current.LastUsedPath = folderOpen.FileNames.ElementAt(0);
            this.BeginDisplayModels(new PathPackage(folderOpen.FileNames, policy));
        }

        public ICommand SelectFolderToHashCmd
        {
            get
            {
                if (this.selectFolderToHashCmd is null)
                {
                    this.selectFolderToHashCmd = new RelayCommand(this.SelectFolderToHashAction);
                }
                return this.selectFolderToHashCmd;
            }
        }

        private void CancelDisplayedModelsAction(object param)
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

        public ICommand CancelDisplayedModelsCmd
        {
            get
            {
                if (this.cancelDisplayedModelsCmd is null)
                {
                    this.cancelDisplayedModelsCmd = new RelayCommand(this.CancelDisplayedModelsAction);
                }
                return this.cancelDisplayedModelsCmd;
            }
        }

        private void PauseDisplayedModelsAction(object param)
        {
            foreach (HashViewModel model in HashViewModels)
            {
                model.PauseOrContinueModel(PauseMode.Pause);
            }
        }

        public ICommand PauseDisplayedModelsCmd
        {
            get
            {
                if (this.pauseDisplayedModelsCmd is null)
                {
                    this.pauseDisplayedModelsCmd = new RelayCommand(this.PauseDisplayedModelsAction);
                }
                return this.pauseDisplayedModelsCmd;
            }
        }

        private void ContinueDisplayedModelsAction(object param)
        {
            foreach (HashViewModel model in HashViewModels)
            {
                model.PauseOrContinueModel(PauseMode.Continue);
            }
        }

        public ICommand ContinueDisplayedModelsCmd
        {
            get
            {
                if (this.continueDisplayedModelsCmd is null)
                {
                    this.continueDisplayedModelsCmd = new RelayCommand(this.ContinueDisplayedModelsAction);
                }
                return this.continueDisplayedModelsCmd;
            }
        }

        private void PauseSelectedModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    model.PauseOrContinueModel(PauseMode.Pause);
                }
            }
        }

        private void CancelSelectedModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    model.ShutdownModel();
                }
            }
        }

        private void ContinueSelectedModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    model.PauseOrContinueModel(PauseMode.Continue);
                }
            }
        }

        private void RestartSelectedModelsForceAction(object param)
        {
            if (param is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    model.StartupModel(true);
                }
            }
        }

        private void RestartSelectedUnsucceededModelsAction(object param)
        {
            if (param is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    model.StartupModel(false);
                }
            }
        }

        private void RestartSelectedModelsNewLineAction(object param)
        {
            if (param is IList selectedModels)
            {
                IEnumerable<ModelArg> args = selectedModels.Cast<HashViewModel>().Select(i => i.ModelArg);
                this.BeginDisplayModels(args, true);
            }
        }

        public GenericItemModel[] CtrlHashViewModelTaskCmds
        {
            get
            {
                if (this.ctrlHashViewModelTaskCmds is null)
                {
                    this.ctrlHashViewModelTaskCmds = new GenericItemModel[] {
                        new GenericItemModel("暂停任务", new RelayCommand(this.PauseSelectedModelsAction)),
                        new GenericItemModel("继续任务", new RelayCommand(this.ContinueSelectedModelsAction)),
                        new GenericItemModel("取消任务", new RelayCommand(this.CancelSelectedModelsAction)),
                        new GenericItemModel("新增计算", new RelayCommand(this.RestartSelectedModelsNewLineAction)),
                        new GenericItemModel("启动未成功行", new RelayCommand(this.RestartSelectedUnsucceededModelsAction)),
                        new GenericItemModel("重新计算", new RelayCommand(this.RestartSelectedModelsForceAction)),
                    };
                }
                return this.ctrlHashViewModelTaskCmds;
            }
        }

        private void StopEnumeratingPackageAction(object param)
        {
            this.searchCancellation.Cancel();
            this.searchCancellation.Dispose();
            this.searchCancellation = new CancellationTokenSource();
        }

        public ICommand StopEnumeratingPackageCmd
        {
            get
            {
                if (this.stopEnumeratingPackageCmd is null)
                {
                    this.stopEnumeratingPackageCmd = new RelayCommand(this.StopEnumeratingPackageAction);
                }
                return this.stopEnumeratingPackageCmd;
            }
        }

        private void SwitchDisplayedAlgoAction(object param)
        {
            if (param is object[] actionParams && actionParams.Length == 2 &&
                actionParams[0] is AlgoType algo && actionParams[1] is IList selectedModels)
            {
                foreach (HashViewModel model in selectedModels)
                {
                    if (model.AlgoInOutModels != null)
                    {
                        foreach (AlgoInOutModel algoModel in model.AlgoInOutModels)
                        {
                            if (algoModel.AlgoType == algo)
                            {
                                model.CurrentInOutModel = algoModel;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public GenericItemModel[] SwitchDisplayedAlgoCmds
        {
            get
            {
                if (this.switchDisplayedAlgoCmds == null)
                {
                    RelayCommand command = new RelayCommand(this.SwitchDisplayedAlgoAction);
                    this.switchDisplayedAlgoCmds = AlgosPanelModel.ProvidedAlgos.Select(
                        obj => new GenericItemModel(obj.AlgoName, obj.AlgoType, command)).ToArray();
                }
                return this.switchDisplayedAlgoCmds;
            }
        }

        public string HashStringOrChecklistPath
        {
            get
            {
                return this.hashValueStringOrChecklistPath;
            }
            set
            {
                this.SetPropNotify(ref this.hashValueStringOrChecklistPath, value);
            }
        }
    }
}
