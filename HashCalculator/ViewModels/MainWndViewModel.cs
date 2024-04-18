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
        private const int interval = 600;
        private readonly Timer checkStateTimer = null;
        private readonly ModelStarter starter = new ModelStarter(
            Settings.Current.SelectedTaskNumberLimit, 32);
        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private readonly Action<HashModelArg> addModelAction;
        private readonly object displayingModelLock = new object();
        private readonly object changeRunningStateLock = new object();
        private volatile int serial = 0;
        private int computedModelsCount = 0;
        private int tobeComputedModelsCount = 0;
        private List<HashViewModel> displayedModels = new List<HashViewModel>();
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private CancellationTokenSource searchCancellation = new CancellationTokenSource();
        private string hashCheckReport = string.Empty;
        private string hashValueStringOrChecklistPath = null;
        private FilterAndCmdPanel commandPanelInst = null;
        private IList selectedHashViewModels = null;
        private RunningState runningState = RunningState.None;

        private RelayCommand openCommandPanelCmd;
        private RelayCommand openSelectAlgoWndCmd;
        private RelayCommand mainWindowTopmostCmd;
        private RelayCommand clearAllTableLinesCmd;
        private RelayCommand exportHashResultCmd;
        private RelayCommand copyAndRestartModelsCmd;
        private RelayCommand refreshOriginalModelsCmd;
        private RelayCommand forceRefreshOriginalModelsCmd;
        private RelayCommand selectChecklistFileCmd;
        private RelayCommand startCheckHashResultsCmd;
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
        private RelayCommand changeAlgosExportStateCmd;
        private GenericItemModel[] copyModelsHashMenuCmds;
        private GenericItemModel[] copyModelsAllHashesMenuCmds;
        private GenericItemModel[] ctrlHashViewModelTaskCmds;
        private GenericItemModel[] switchDisplayedAlgoCmds;

        private static readonly SizeDelegates sizeDelegates = new SizeDelegates()
        {
            GetWindowWidth = () => Settings.Current.MainWndDelFileProgressWidth,
            SetWindowWidth = width => Settings.Current.MainWndDelFileProgressWidth = width,
            GetWindowHeight = () => Settings.Current.MainWndDelFileProgressHeight,
            SetWindowHeight = height => Settings.Current.MainWndDelFileProgressHeight = height,
        };

        public MainWndViewModel()
        {
            ThisModel = this;
            HashViewModelsViewSrc = new CollectionViewSource();
            HashViewModelsViewSrc.Source = HashViewModels;
            HashViewModelsView = HashViewModelsViewSrc.View;
            Settings.Current.PropertyChanged += this.SettingsPropChangedAction;
            this.addModelAction = new Action<HashModelArg>(this.AddModelAction);
            this.checkStateTimer = new Timer(this.CheckStateAction);
        }

        public static MainWndViewModel ThisModel
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
            set => this.SetPropNotify(ref this.hashCheckReport, value);
        }

        public IList SelectedHashViewModels
        {
            get => this.selectedHashViewModels;
            set
            {
                if (value != null)
                {
                    foreach (HashViewModel model in HashViewModels)
                    {
                        model.IsExecutionTarget = value.Contains(model);
                    }
                }
                this.SetPropNotify(ref this.selectedHashViewModels, value);
            }
        }

        public string HashStringOrChecklistPath
        {
            get => this.hashValueStringOrChecklistPath;
            set => this.SetPropNotify(ref this.hashValueStringOrChecklistPath, value);
        }

        public RunningState State
        {
            get => this.runningState;
            set
            {
                if ((this.runningState != RunningState.Started) && value == RunningState.Started)
                {
                    this.Report = string.Empty;
                    this.SetPropNotify(ref this.runningState, value);
                }
                else if (this.runningState == RunningState.Started && value == RunningState.Stopped)
                {
                    this.GenerateFileHashCheckReport();
                    this.SetPropNotify(ref this.runningState, value);
                }
            }
        }

        public int TobeComputedModelsCount
        {
            get => this.tobeComputedModelsCount;
            set => this.SetPropNotify(ref this.tobeComputedModelsCount, value);
        }

        private CancellationTokenSource Cancellation
        {
            get => this._cancellation;
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

        public void CheckHashUseClipboardText()
        {
            if (HashViewModels.AnyItem() &&
                CommonUtils.ClipboardGetText(out string clipboardText) &&
                clipboardText.Length >= Settings.Current.MinCopiedCharsToTriggerHashCheck &&
                clipboardText.Length <= Settings.Current.MaxCopiedCharsToTriggerHashCheck)
            {
                HashChecklist checklist = File.Exists(clipboardText) ?
                    HashChecklist.File(clipboardText) : HashChecklist.Text(clipboardText);
                if (checklist.ReasonForFailure == null)
                {
                    this.HashStringOrChecklistPath = clipboardText;
                    if (this.State != RunningState.Started && this.CheckFilesHashBasedOnStringOrChecklist(checklist) &&
                        Settings.Current.SwitchMainWndFgWhenNewHashCopied)
                    {
                        CommonUtils.ShowWindowForeground(MainWindow.ProcessId);
                    }
                }
            }
        }

        private void CheckStateAction(object state)
        {
            lock (this.changeRunningStateLock)
            {
                this.TobeComputedModelsCount -= this.computedModelsCount;
                this.computedModelsCount = 0;
                if (this.TobeComputedModelsCount == 0)
                {
                    synchronization.Invoke(() => { this.State = RunningState.Stopped; });
                    this.checkStateTimer.Change(-1, -1);
                }
            }
        }

        private void ModelReleasedAction(HashViewModel model)
        {
            lock (this.changeRunningStateLock)
            {
                ++this.computedModelsCount;
            }
        }

        private void ModelCapturedAction(HashViewModel model)
        {
            // 在最外层套上 synchronization.Invoke 在主线程执行逻辑虽然也能达到锁效果，
            // 但每次更改 TobeComputedModelsCount 的值都要 Invoke 占用主线程资源，没有必要
            lock (this.changeRunningStateLock)
            {
                if (++this.TobeComputedModelsCount == 1)
                {
                    synchronization.Invoke(() => { this.State = RunningState.Started; });
                    this.checkStateTimer.Change(interval, interval);
                }
            }
        }

        private void AddModelAction(HashModelArg arg)
        {
            HashViewModel model = new HashViewModel(++this.serial, arg);
            this.displayedModels.Add(model);
            model.ModelCapturedEvent += this.ModelCapturedAction;
            model.ModelCapturedEvent += this.starter.PendingModel;
            model.ModelReleasedEvent += this.ModelReleasedAction;
            HashViewModels.Add(model);
            model.StartupModel(false);
        }

        public async void BeginDisplayModels(IEnumerable<HashViewModel> models, bool resetAlg)
        {
            CancellationToken token = this.Cancellation.Token;
            await Task.Run(() =>
            {
                lock (this.displayingModelLock)
                {
                    foreach (HashModelArg arg in models.Select(i => i.Arguments))
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        if (resetAlg)
                        {
                            arg.PresetAlgos = null;
                        }
                        synchronization.Invoke(this.addModelAction, DispatcherPriority.Loaded, arg);
                    }
                }
            }, token);
        }

        public async void BeginDisplayModels(params PathPackage[] packages)
        {
            CancellationToken token = this.Cancellation.Token;
            await Task.Run(() =>
            {
                CancellationToken stopSearchingToken = this.searchCancellation.Token;
                lock (this.displayingModelLock)
                {
                    foreach (PathPackage package in packages)
                    {
                        if (token.IsCancellationRequested ||
                            stopSearchingToken.IsCancellationRequested)
                        {
                            break;
                        }
                        package.StopSearchingToken = stopSearchingToken;
                        foreach (HashModelArg arg in package)
                        {
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }
                            synchronization.Invoke(this.addModelAction, DispatcherPriority.Loaded, arg);
                        }
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
            StringBuilder builder = new StringBuilder();
            builder.Append($"总行数：{HashViewModels.Count}\n已成功：{succeeded}\n");
            builder.Append($"已失败：{hasFailed}\n已取消：{canceled}\n\n");
            builder.Append($"校验汇总：\n算法数：{totalHash}\n");
            builder.Append($"已匹配：{matched}\n不匹配：{mismatch}\n");
            builder.Append($"不确定：{uncertain}\n无关联：{unrelated}\n未校验：{noresult}");
            this.Report = builder.ToString();
        }

        /// <summary>
        /// 需要立即响应的设置变更
        /// </summary>
        private void SettingsPropChangedAction(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.RunInMultiInstMode))
            {
                Initializer.RunMultiMode = Settings.Current.RunInMultiInstMode;
            }
            else if (e.PropertyName == nameof(Settings.Current.SelectedTaskNumberLimit))
            {
                this.starter.BeginAdjust(Settings.Current.SelectedTaskNumberLimit);
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

        private void CopyModelsHashValueAction(object param, OutputType outputType, bool copyAll)
        {
            if (param is IList selectedModels && selectedModels.AnyItem())
            {
                StringBuilder stringBuilder = new StringBuilder();
                string format = Settings.Current.GenerateTextInFormat ?
                    Settings.Current.FormatForGenerateText : null;
                foreach (HashViewModel model in selectedModels)
                {
                    if (model.GenerateTextInFormat(format, outputType, copyAll, endLine: true, seeExport: false,
                        Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
                    {
                        stringBuilder.Append(text);
                    }
                }
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    CommonUtils.ClipboardSetText(stringBuilder.ToString());
                }
            }
        }

        private void CopyModelsHashBase64Action(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BASE64, false);
        }

        private void CopyModelsHashBinUpperAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryUpper, false);
        }

        private void CopyModelsHashBinLowerAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryLower, false);
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

        private void CopyModelsAllBase64HashesAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BASE64, true);
        }

        private void CopyModelsAllBinUpperHashesAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryUpper, true);
        }

        private void CopyModelsAllBinLowerHashesAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.BinaryLower, true);
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
                bool fullPathCopied = false;
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < count; ++i)
                {
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    if (i != 0)
                    {
                        stringBuilder.AppendLine();
                    }
                    if (copyName)
                    {
                        stringBuilder.Append(model.Information.Name);
                    }
                    else if (!model.Arguments.Deprecated)
                    {
                        fullPathCopied = true;
                        stringBuilder.Append(model.Information.FullName);
                    }
                }
                if (stringBuilder.Length != 0)
                {
                    CommonUtils.ClipboardSetText(stringBuilder.ToString());
                }
                if (!copyName && !fullPathCopied)
                {
                    MessageBox.Show(this.OwnerWnd, "文件不存在所以完整路径没有被复制", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
                    if (!Path.IsPathRooted(model.Arguments.FilePath))
                    {
                        continue;
                    }
                    foreach (string key in groupByDir.Keys)
                    {
                        if (model.Information.ParentSameWith(key))
                        {
                            isMatched = true;
                            groupByDir[key].Add(model.Information.Name);
                            break;
                        }
                    }
                    if (!isMatched)
                    {
                        groupByDir[model.Information.DirectoryName] = new List<string> {
                            model.Information.Name };
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
                    if (!File.Exists(model.Information.FullName))
                    {
                        continue;
                    }
                    SHELL32.ShellExecuteW(MainWindow.WndHandle, "open",
                        model.Information.FullName, null,
                        Path.GetDirectoryName(model.Information.FullName), ShowCmd.SW_SHOWNORMAL);
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
                    if (File.Exists(model.Information.FullName))
                    {
                        var shellExecuteInfo = new SHELLEXECUTEINFOW();
                        shellExecuteInfo.cbSize = Marshal.SizeOf(shellExecuteInfo);
                        shellExecuteInfo.fMask = SEMaskFlags.SEE_MASK_INVOKEIDLIST;
                        shellExecuteInfo.hwnd = MainWindow.WndHandle;
                        shellExecuteInfo.lpVerb = "properties";
                        shellExecuteInfo.lpFile = model.Information.FullName;
                        shellExecuteInfo.lpDirectory = model.Information.DirectoryName;
                        shellExecuteInfo.nShow = ShowCmd.SW_SHOWNORMAL;
                        SHELL32.ShellExecuteExW(ref shellExecuteInfo);
                    }
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

        private async void DeleteSelectedModelsFileAction(object param)
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
                DoubleProgressModel progress = new DoubleProgressModel(sizeDelegates)
                {
                    IsCancelled = true,
                    TotalCount = count,
                    SubProgressVisibility = Visibility.Collapsed,
                    TotalProgressVisibility = Visibility.Collapsed,
                    TotalString = "正在删除文件，请稍候...",
                };
                DoubleProgressWindow progressWindow = new DoubleProgressWindow(progress)
                {
                    Owner = this.OwnerWnd,
                };
                HashViewModel[] targets = selectedModels.Cast<HashViewModel>().ToArray();
                foreach (HashViewModel model in targets)
                {
                    model.ShutdownModelWait();
                    HashViewModels.Remove(model);
                }
                Task<string> deleteFileTask = Task.Run(() =>
                {
                    try
                    {
                        if (Settings.Current.PermanentlyDeleteFiles)
                        {
                            List<string> fileNameList = new List<string>();
                            foreach (HashViewModel model in targets)
                            {
                                try
                                {
                                    model.Information.Delete();
                                }
                                catch (Exception)
                                {
                                    fileNameList.Add(model.FileName);
                                }
                            }
                            if (fileNameList.Any())
                            {
                                return "以下文件删除失败：\n" + '\n'.Join(fileNameList);
                            }
                            return default(string);
                        }
                        else
                        {
                            string allInOneStr = '\0'.Join(targets.Select(i => i.Information.FullName));
                            if (!CommonUtils.SendToRecycleBin(MainWindow.WndHandle, allInOneStr))
                            {
                                return "移动文件到回收站失败，可能部分文件未移动！";
                            }
                            return default(string);
                        }
                    }
                    catch (Exception ex)
                    {
                        return $"删除文件或移动文件到回收站的过程出现异常：{ex.Message}";
                    }
                    finally
                    {
                        progress.AutoClose = true;
                        synchronization.Invoke(() =>
                        {
                            progressWindow.DialogResult = false;
                        });
                    }
                });
                progressWindow.ShowDialog();
                string exceptionMessage = await deleteFileTask;
                if (!string.IsNullOrEmpty(exceptionMessage))
                {
                    MessageBox.Show(this.OwnerWnd, $"{exceptionMessage}", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
                    this.displayedModels.Remove(model);
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
            this.displayedModels.Clear();
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
            if (!HashViewModels.Any(i => i.Result == HashResult.Succeeded))
            {
                MessageBox.Show(this.OwnerWnd, "主窗口列表中没有可以导出的结果。", "提示");
                return;
            }
            if (Settings.Current.TemplatesForExport?.Any() != true)
            {
                MessageBox.Show(this.OwnerWnd, "没有导出方案可用，请到【导出结果设置】中添加。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Settings.Current.AskUserHowToExportResultsEveryTime)
            {
                HowToExportResults howToExportResults = new HowToExportResults()
                {
                    Owner = this.OwnerWnd,
                };
                if (howToExportResults.ShowDialog() != true)
                {
                    return;
                }
            }
            var usedModels = new List<TemplateForExportModel>();
            StringBuilder filterStringBuilder = new StringBuilder();
            foreach (TemplateForExportModel model in Settings.Current.TemplatesForExport)
            {
                if (model.GetFilterFormat(Settings.Current.EachAlgoExportedToSeparateFile)
                    is string filterFormat)
                {
                    usedModels.Add(model);
                    filterStringBuilder.Append(filterFormat);
                    filterStringBuilder.Append('|');
                }
            }
            if (filterStringBuilder.Length > 0)
            {
                filterStringBuilder.Remove(filterStringBuilder.Length - 1, 1);
            }
            if (!usedModels.Any())
            {
                MessageBox.Show(this.OwnerWnd,
                    "没有可用方案，可能方案的扩展名中存在不能用作文件名的字符，请到【导出结果设置】中修改。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                string presetName = Settings.Current.LastSavedName;
                string defaultNoExt = "hashsums";
                string nameNoExt = string.IsNullOrEmpty(presetName) ? defaultNoExt :
                    Path.GetFileNameWithoutExtension(presetName);
                if (string.IsNullOrEmpty(nameNoExt))
                {
                    nameNoExt = defaultNoExt;
                }
                presetName = Settings.Current.EachAlgoExportedToSeparateFile ? nameNoExt :
                    $"{nameNoExt}{usedModels[0].Extension}";
                SaveFileDialog saveFileDialog = new SaveFileDialog()
                {
                    ValidateNames = true,
                    Filter = filterStringBuilder.ToString(),
                    FileName = presetName,
                    InitialDirectory = Settings.Current.LastUsedPath,
                };
                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }
                Settings.Current.LastSavedName = Path.GetFileName(saveFileDialog.FileName);
                Settings.Current.LastUsedPath = Path.GetDirectoryName(saveFileDialog.FileName);
                OutputType output = OutputType.Unknown;
                if (Settings.Current.UseDefaultOutputTypeWhenExporting)
                {
                    output = Settings.Current.SelectedOutputType;
                }
                bool all = Settings.Current.HowToExportHashValues != ExportAlgo.Current;
                TemplateForExportModel model = usedModels.ElementAt(saveFileDialog.FilterIndex - 1);
                Encoding encoding = model.GetEncoding();
                if (Settings.Current.EachAlgoExportedToSeparateFile)
                {
                    this.EachAlgoExportedToSeparateFile(saveFileDialog.FileName, encoding,
                        model.Template, output);
                }
                else
                {
                    this.AlgorithmResultsExportedToSameFile(saveFileDialog.FileName, encoding,
                        model.Template, output, all);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.OwnerWnd, $"导出哈希值失败，异常信息：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EachAlgoExportedToSeparateFile(string file, Encoding encoding, string format,
            OutputType output)
        {
            var algoTypes = new Dictionary<AlgoType, string>();
            List<HashViewModel> validHashViews = new List<HashViewModel>();
            foreach (HashViewModel hashView in HashViewModels)
            {
                if (hashView.Result == HashResult.Succeeded &&
                    hashView.AlgoInOutModels != null)
                {
                    foreach (AlgoInOutModel inOutModel in hashView.AlgoInOutModels)
                    {
                        if (!algoTypes.ContainsKey(inOutModel.AlgoType))
                        {
                            string fileFullPath = Path.ChangeExtension(file,
                                inOutModel.AlgoName.ToLower());
                            algoTypes.Add(inOutModel.AlgoType, fileFullPath);
                        }
                    }
                    validHashViews.Add(hashView);
                }
            }
            List<string> existedFiles = new List<string>();
            foreach (string filePath in algoTypes.Values)
            {
                if (File.Exists(filePath))
                {
                    existedFiles.Add(filePath);
                }
            }
            if (existedFiles.Count > 0)
            {
                string paths = '\n'.Join(existedFiles);
                if (MessageBox.Show(this.OwnerWnd,
                    $"已存在以下文件，继续导出将会覆盖原文件，仍然要导出吗？\n{paths}", "警告",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            HashSet<AlgoType> algoTypesSet = algoTypes.Keys.ToHashSet();
            foreach (HashViewModel hashView in validHashViews)
            {
                HashSet<AlgoType> typesSet = hashView.AlgoInOutModels.Select(
                    i => i.AlgoType).ToHashSet();
                if (!algoTypesSet.SetEquals(typesSet))
                {
                    if (MessageBox.Show(this.OwnerWnd,
                        "并非所有行包含的算法都一样，如果仍要导出结果，则导出的每个清单里 包含的" +
                        "文件数量不一样，仍然要导出吗？", "警告",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        break;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            foreach (KeyValuePair<AlgoType, string> keyValuePair in algoTypes)
            {
                using (FileStream fileStream = File.Create(keyValuePair.Value))
                using (StreamWriter streamWriter = new StreamWriter(fileStream, encoding))
                {
                    foreach (HashViewModel hashView in validHashViews)
                    {
                        foreach (AlgoInOutModel inOutModel in hashView.AlgoInOutModels)
                        {
                            if (inOutModel.AlgoType == keyValuePair.Key)
                            {
                                if (inOutModel.GenerateTextInFormat(hashView, format, output, endLine: true,
                                seeExport: true, casedAlgName: false) is string text)
                                {
                                    streamWriter.Write(text);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void AlgorithmResultsExportedToSameFile(string file, Encoding encoding, string format,
            OutputType output, bool all)
        {
            using (FileStream fileStream = File.Create(file))
            using (StreamWriter streamWriter = new StreamWriter(fileStream, encoding))
            {
                foreach (HashViewModel hm in HashViewModels.Where(i => i.Result == HashResult.Succeeded))
                {
                    if (hm.GenerateTextInFormat(format, output, all, endLine: true, seeExport: true,
                        casedName: false) is string text)
                    {
                        streamWriter.Write(text);
                    }
                }
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
                if (this.displayedModels.Any())
                {
                    List<HashViewModel> args = this.displayedModels;
                    this.displayedModels = new List<HashViewModel>();
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

        public bool CheckFilesHashBasedOnStringOrChecklist(HashChecklist checklist)
        {
            HashChecklist localChecklist;
            if (checklist == null)
            {
                if (string.IsNullOrEmpty(this.HashStringOrChecklistPath))
                {
                    this.GenerateOriginFileHashCheckReport();
                    return false;
                }
                // HashStringOrChecklistPath 不是一个文件
                if (!File.Exists(this.HashStringOrChecklistPath))
                {
                    localChecklist = HashChecklist.Text(this.HashStringOrChecklistPath);
                }
                // HashStringOrChecklistPath 是一个文件，但哈希结果列表不是空
                else if (HashViewModels.Any())
                {
                    localChecklist = HashChecklist.File(this.HashStringOrChecklistPath);
                }
                // HashStringOrChecklistPath 是一个文件，且哈希结果列表也是空
                else
                {
                    HashChecklist newChecklist = HashChecklist.File(this.HashStringOrChecklistPath);
                    if (newChecklist.ReasonForFailure != null)
                    {
                        MessageBox.Show(MainWindow.This, newChecklist.ReasonForFailure, "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        // 这里添加要计算哈希值的文件时，看作以多选文件的方式添，所以
                        // PathPackage 的 parent 参数应是 HashStringOrChecklistPath 所在目录
                        string checklistDir = Path.GetDirectoryName(this.HashStringOrChecklistPath);
                        this.BeginDisplayModels(new PathPackage(checklistDir, checklistDir, newChecklist,
                            Settings.Current.SelectedSearchMethodForChecklist));
                    }
                    return newChecklist.ReasonForFailure == null;
                }
            }
            else
            {
                localChecklist = checklist;
            }
            if (localChecklist.ReasonForFailure == null)
            {
                foreach (HashViewModel hm in HashViewModels)
                {
                    hm.SetHashCheckResultForModel(localChecklist);
                }
                this.GenerateFileHashCheckReport();
            }
            else
            {
                this.GenerateOriginFileHashCheckReport();
                MessageBox.Show(MainWindow.This, localChecklist.ReasonForFailure, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return localChecklist.ReasonForFailure == null;
        }

        private void SelectChecklistFileAction(object param)
        {
            CommonOpenFileDialog openFile = new CommonOpenFileDialog
            {
                Title = "选择【哈希值清单】文件",
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

        private void StartCheckHashResultsAction(object param)
        {
            this.CheckFilesHashBasedOnStringOrChecklist(null);
        }

        public ICommand StartCheckHashResultsCmd
        {
            get
            {
                if (this.startCheckHashResultsCmd is null)
                {
                    this.startCheckHashResultsCmd = new RelayCommand(this.StartCheckHashResultsAction);
                }
                return this.startCheckHashResultsCmd;
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
            string parentDir = Path.GetDirectoryName(fileOpen.FileNames.First());
            Settings.Current.LastUsedPath = parentDir;
            this.BeginDisplayModels(new PathPackage(parentDir, fileOpen.FileNames,
                Settings.Current.SelectedSearchMethodForDragDrop));
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
            SearchMethod searchMethod = Settings.Current.SelectedSearchMethodForDragDrop;
            if (searchMethod == SearchMethod.DontSearch)
            {
                searchMethod = SearchMethod.Children;
            }
            string firstDir = folderOpen.FileNames.First();
            // firstDir 是分区根目录时 GetDirectoryName 返回 null
            string parentDir = Path.GetDirectoryName(firstDir) ?? firstDir;
            Settings.Current.LastUsedPath = parentDir;
            this.BeginDisplayModels(new PathPackage(parentDir, folderOpen.FileNames, searchMethod));
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
            this.Cancellation?.Cancel();
            foreach (HashViewModel model in HashViewModels)
            {
                model.ShutdownModel();
            }
            this.Cancellation?.Dispose();
            this.Cancellation = new CancellationTokenSource();
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
                this.BeginDisplayModels(selectedModels.Cast<HashViewModel>(), true);
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

        private void ChangeAlgosExportStateAction(object param)
        {
            if (param is HashViewModel model && model.CurrentInOutModel != null)
            {
                if (Settings.Current.ExportInMainControlsChildExports)
                {
                    if (model.AlgoInOutModels?.AnyItem() == true)
                    {
                        bool export = !model.CurrentInOutModel.Export;
                        foreach (AlgoInOutModel inOut in model.AlgoInOutModels)
                        {
                            inOut.Export = export;
                        }
                    }
                }
                else
                {
                    model.CurrentInOutModel.Export = !model.CurrentInOutModel.Export;
                }
            }
        }

        public ICommand ChangeAlgosExportStateCmd
        {
            get
            {
                if (this.changeAlgosExportStateCmd == null)
                {
                    this.changeAlgosExportStateCmd = new RelayCommand(this.ChangeAlgosExportStateAction);
                }
                return this.changeAlgosExportStateCmd;
            }
        }
    }
}
