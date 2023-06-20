using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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
using System.Windows.Resources;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class MainWndViewModel : NotifiableModel
    {
        private readonly Basis MainBasis = new Basis();
        private readonly ModelStarter starter =
            new ModelStarter((int)Settings.Current.SelectedTaskNumberLimit, 8);
        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private delegate void AddModelDelegate(ModelArg arg);
        private readonly AddModelDelegate addModelAction;
        private volatile int serial = 0;
        private int tobeComputedModelsCount = 0;
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private List<ModelArg> displayedFiles = new List<ModelArg>();
        private string hashCheckReport = string.Empty;
        private QueueState queueState = QueueState.None;
        private readonly object displayingModelLock = new object();
        private readonly object displayModelRequestLock = new object();
        private readonly object tobeComputedModelsCountLock = new object();
        private RelayCommand mainWindowTopmostCmd;
        private RelayCommand clearAllTableLinesCmd;
        private RelayCommand exportAsTextFileCmd;
        private RelayCommand refreshHashNewLinesCmd;
        private RelayCommand refreshCurrentHashCmd;
        private RelayCommand refreshCurHashForceCmd;
        private RelayCommand startVerifyHashValueCmd;
        private RelayCommand openSettingsPanelCmd;
        private RelayCommand openHelpBrowserWndCmd;
        private RelayCommand selectFilesToHashCmd;
        private RelayCommand selectFolderToHashCmd;
        private RelayCommand cancelDisplayedModelsCmd;
        private RelayCommand pauseDisplayedModelsCmd;
        private RelayCommand continueDisplayedModelsCmd;
        private RelayCommand copyModelsHashStringCmd;
        private RelayCommand copyFilesFullPathCmd;
        private RelayCommand openFolderSelectItemsCmd;
        private RelayCommand openModelsFilePathCmd;
        private RelayCommand openFilesPropertyCmd;
        private RelayCommand refreshAllOutputTypeCmd;
        private RelayCommand deleteSelectedModelsFileCmd;
        private RelayCommand removeSelectedModelsCmd;
        private ControlItem[] copyModelsHashMenuCmds;
        private ControlItem[] hashModelTasksCtrlCmds;
        private ControlItem[] hashModelsCalculationCmds;

        public MainWndViewModel()
        {
            Settings.Current.PropertyChanged += this.PropChangedAction;
            this.addModelAction = new AddModelDelegate(this.AddModelAction);
        }

        public Window Parent { get; set; }

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
                    synchronization.Invoke(() => { this.Report = string.Empty; });
                    this.SetPropNotify(ref this.queueState, value);
                }
                else if (this.queueState == QueueState.Started && value == QueueState.Stopped)
                {
                    this.GenerateVerificationReport();
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

        private void AddModelAction(ModelArg arg)
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
            lock (this.tobeComputedModelsCountLock)
            {
                if (this.TobeComputedModelsCount++ != 0)
                {
                    return;
                }
                synchronization.Invoke(() => { this.State = QueueState.Started; });
            }
        }

        private void ModelReleasedAction(HashViewModel model)
        {
            lock (this.tobeComputedModelsCountLock)
            {
                if (--this.TobeComputedModelsCount != 0)
                {
                    return;
                }
                synchronization.Invoke(() => { this.State = QueueState.Stopped; });
            }
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
                        synchronization.Invoke(this.addModelAction, arg);
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
                        synchronization.Invoke(this.addModelAction, arg);
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

        private void PropChangedAction(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.SelectedTaskNumberLimit))
            {
                this.starter.BeginAdjust((int)Settings.Current.SelectedTaskNumberLimit);
            }
        }

        public string StartVerifyToolTip { get; } =
            "当面板为空时，如果校验依据选择的是通用格式的哈希值文本文件，则：\n" +
            "点击 [校验] 后程序会自动解析文件并在相同目录下寻找要计算哈希值的文件完成计算并显示校验结果。\n" +
            "通用格式的哈希值文件请参考程序 [导出结果] 功能导出的文件的内容排布格式。";

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

        private void CopyModelsHashValueAction(object param, OutputType output)
        {
            if (param is IList selectedModels)
            {
                int count = selectedModels.Count;
                if (count == 0)
                {
                    return;
                }
                StringBuilder hashValueStringBuilder = new StringBuilder();
                for (int i = 0; i < count; ++i)
                {
                    HashViewModel model = (HashViewModel)selectedModels[i];
                    string formatedHashValue;
                    if (output != OutputType.Unknown)
                    {
                        formatedHashValue =
                            (string)HashBytesOutputTypeCvt.Convert(
                                model.Hash, output);
                    }
                    else if (model.SelectedOutputType == OutputType.Unknown)
                    {
                        formatedHashValue =
                            (string)HashBytesOutputTypeCvt.Convert(
                                model.Hash, Settings.Current.SelectedOutputType);
                    }
                    else
                    {
                        formatedHashValue = model.HashString;
                    }
                    if (i != 0)
                    {
                        hashValueStringBuilder.AppendLine();
                    }
                    hashValueStringBuilder.Append(formatedHashValue);
                }
                if (hashValueStringBuilder.Length != 0)
                {
                    Clipboard.SetText(hashValueStringBuilder.ToString());
                }
            }
        }

        public ControlItem[] CopyModelsHashMenuCmds
        {
            get
            {
                if (this.copyModelsHashMenuCmds is null)
                {
                    this.copyModelsHashMenuCmds = new ControlItem[] {
                        new ControlItem("Base64 格式", new RelayCommand(this.CopyModelsHashBase64Action)),
                        new ControlItem("十六进制大写", new RelayCommand(this.CopyModelsHashBinUpperAction)),
                        new ControlItem("十六进制小写", new RelayCommand(this.CopyModelsHashBinLowerAction)),
                    };
                }
                return this.copyModelsHashMenuCmds;
            }
        }

        private void CopyModelsHashStringAction(object param)
        {
            this.CopyModelsHashValueAction(param, OutputType.Unknown);
        }

        public ICommand CopyModelsHashStringCmd
        {
            get
            {
                if (this.copyModelsHashStringCmd is null)
                {
                    this.copyModelsHashStringCmd = new RelayCommand(this.CopyModelsHashStringAction);
                }
                return this.copyModelsHashStringCmd;
            }
        }

        private void CopyFilesFullPathAction(object param)
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

        public ICommand CopyFilesFullPathCmd
        {
            get
            {
                if (this.copyFilesFullPathCmd is null)
                {
                    this.copyFilesFullPathCmd = new RelayCommand(this.CopyFilesFullPathAction);
                }
                return this.copyFilesFullPathCmd;
            }
        }

        private void OpenFolderSelectItemsAction(object param)
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
                        ShowCmds.SW_SHOWNORMAL);
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
                    shellExecuteInfo.nShow = ShowCmds.SW_SHOWNORMAL;
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
                    MessageBox.Show(this.Parent,
                        $"文件删除失败：\n{model.FileInfo.FullName}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (!CommonUtils.SendToRecycleBin(
                        MainWindow.WndHandle, model.FileInfo.FullName))
                {
                    MessageBox.Show(this.Parent,
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
                    this.Parent, deleteFileTip, "提示",
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
                    // 对 HashViewModels 的增删操作必定是在主线程上的，不用加锁
                    model.ShutdownModel();
                    HashViewModels.Remove(model);
                }
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
                    // 对 HashViewModels 的增删操作必定是在主线程上的，不用加锁
                    HashViewModels.Remove(model);
                }
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

        private void ExportAsTextFileAction(object param)
        {
            if (!HashViewModels.Any())
            {
                MessageBox.Show(this.Parent, "列表中没有任何可以导出的条目。", "提示");
                return;
            }
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = "hashsums.txt",
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (sf.ShowDialog() != true)
            {
                return;
            }
            Settings.Current.LastUsedPath = Path.GetDirectoryName(sf.FileName);
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (HashViewModel hm in HashViewModels)
                    {
                        if (!(hm.IsSucceeded && hm.Export))
                        {
                            continue;
                        }
                        string hash = HashBytesOutputTypeCvt.Convert(
                                hm.Hash, Settings.Current.SelectedOutputType) as string;
                        sw.WriteLine($"#{AlgoMap.GetAlgoName(hm.HashAlgoType)} *{hash} *{hm.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.Parent, $"哈希值导出失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public ICommand ExportAsTextFileCmd
        {
            get
            {
                if (this.exportAsTextFileCmd is null)
                {
                    this.exportAsTextFileCmd = new RelayCommand(this.ExportAsTextFileAction);
                }
                return this.exportAsTextFileCmd;
            }
        }

        public void RestartModels(bool newLines, bool force)
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

        private void RefreshHashNewLinesAction(object param)
        {
            this.RestartModels(true, false);
        }

        public ICommand RefreshHashNewLinesCmd
        {
            get
            {
                if (this.refreshHashNewLinesCmd is null)
                {
                    this.refreshHashNewLinesCmd = new RelayCommand(this.RefreshHashNewLinesAction);
                }
                return this.refreshHashNewLinesCmd;
            }
        }

        private void RefreshCurrentHashAction(object param)
        {
            this.RestartModels(false, false);
        }

        public ICommand RefreshCurrentHashCmd
        {
            get
            {
                if (this.refreshCurrentHashCmd is null)
                {
                    this.refreshCurrentHashCmd = new RelayCommand(this.RefreshCurrentHashAction);
                }
                return this.refreshCurrentHashCmd;
            }
        }

        private void RefreshCurHashForceAction(object param)
        {
            this.RestartModels(false, true);
        }

        public ICommand RefreshCurHashForceCmd
        {
            get
            {
                if (this.refreshCurHashForceCmd is null)
                {
                    this.refreshCurHashForceCmd = new RelayCommand(this.RefreshCurHashForceAction);
                }
                return this.refreshCurHashForceCmd;
            }
        }

        private void StartVerificationAction(object param)
        {
            string pathOrHash = param as string;
            if (string.IsNullOrEmpty(pathOrHash))
            {
                MessageBox.Show(this.Parent, "没有输入哈希值校验依据。", "提示");
                return;
            }
            bool basisUpdated = false;
            if (!File.Exists(pathOrHash))
            {
                basisUpdated = this.MainBasis.UpdateWithHash(pathOrHash);
            }
            else
            {
                if (HashViewModels.Any())
                {
                    basisUpdated = this.MainBasis.UpdateWithFile(pathOrHash);
                }
                else
                {
                    Basis newBasis = new Basis(pathOrHash);
                    this.BeginDisplayModels(
                        new PathPackage(new string[] { Path.GetDirectoryName(pathOrHash) },
                            Settings.Current.SelectedQVSPolicy, newBasis));
                }
            }
            if (basisUpdated)
            {
                foreach (HashViewModel hm in HashViewModels)
                {
                    if (!hm.IsSucceeded)
                    {
                        hm.CmpResult = CmpRes.NoResult;
                    }
                    else
                    {
                        hm.CmpResult = this.MainBasis.Verify(hm.FileName, hm.Hash);
                    }
                }
                this.GenerateVerificationReport();
            }
        }

        public ICommand StartVerifyHashValueCmd
        {
            get
            {
                if (this.startVerifyHashValueCmd is null)
                {
                    this.startVerifyHashValueCmd = new RelayCommand(this.StartVerificationAction);
                }
                return this.startVerifyHashValueCmd;
            }
        }

        private void OpenSettingsPanelAction(object param)
        {
            Window settingsWindow = new SettingsPanel() { Owner = this.Parent };
            settingsWindow.ShowDialog();
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

        private void OpenHelpBrowserWndAction(object param)
        {
            try
            {
                StreamResourceInfo sri = Application.GetResourceStream(
                    new Uri("WebPages/Help.html", UriKind.Relative));
                using (sri.Stream)
                {
                    string htmlPath = Path.ChangeExtension(Path.GetTempFileName(), "html");
                    using (Stream fs = File.OpenWrite(htmlPath))
                    {
                        byte[] streamBuffer = new byte[sri.Stream.Length];
                        sri.Stream.Read(streamBuffer, 0, streamBuffer.Length);
                        fs.Write(streamBuffer, 0, streamBuffer.Length);
                        NativeFunctions.ShellExecuteW(
                            MainWindow.WndHandle, "open", htmlPath, null, null, ShowCmds.SW_NORMAL);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    this.Parent, "打开帮助页面失败~", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public ICommand OpenHelpBrowserWndCmd
        {
            get
            {
                if (this.openHelpBrowserWndCmd is null)
                {
                    this.openHelpBrowserWndCmd = new RelayCommand(this.OpenHelpBrowserWndAction);
                }
                return this.openHelpBrowserWndCmd;
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

        public ControlItem[] HashModelTasksCtrlCmds
        {
            get
            {
                if (this.hashModelTasksCtrlCmds is null)
                {
                    this.hashModelTasksCtrlCmds = new ControlItem[] {
                        new ControlItem("暂停", new RelayCommand(this.PauseSelectedModelsAction)),
                        new ControlItem("继续", new RelayCommand(this.ContinueSelectedModelsAction)),
                        new ControlItem("取消", new RelayCommand(this.CancelSelectedModelsAction)),
                    };
                }
                return this.hashModelTasksCtrlCmds;
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
                this.BeginDisplayModels(args);
            }
        }

        public ControlItem[] HashModelsCalculationCmds
        {
            get
            {
                if (this.hashModelsCalculationCmds is null)
                {
                    this.hashModelsCalculationCmds = new ControlItem[] {
                        new ControlItem("新增计算", new RelayCommand(this.RestartSelectedModelsNewLineAction)),
                        new ControlItem("启动未成功项", new RelayCommand(this.RestartSelectedUnsucceededModelsAction)),
                        new ControlItem("重新计算", new RelayCommand(this.RestartSelectedModelsForceAction)),
                    };
                }
                return this.hashModelsCalculationCmds;
            }
        }
    }
}
