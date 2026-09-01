using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HashCalculator.Others;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Windows;
using Microsoft.Win32;
using Wpf.Ui;
using Wpfctrls = Wpf.Ui.Controls;

namespace HashCalculator.ViewModels.Pages;

public class HomeViewModel : BaseViewModel
{
    private readonly Action<HashModelArg> addModelAction;
    private readonly Action<IEnumerable<HashModelArg>> addModelItemsAction;
    private readonly Lock displayingModelLock = new Lock();
    private volatile int serial = 0;
    /// <summary>
    /// 停止添加行的标志。<br/>
    /// 添加行的委托是同步投递到界面线程的，取消/清空时已经投递但尚未执行的那一个无法撤销，
    /// 而 Cancellation 在取消时会被立即重建，那批委托无法据此判断"刚刚取消过"，故另设此标志，让它们整批跳过。
    /// </summary>
    private volatile bool stopAddingModels = false;
    private int pendingModelsCount = 0;
    private object selectedHashVieModel = -1;
    private List<HashViewModel> displayedModels = new List<HashViewModel>();
    private CancellationTokenSource _cancellation = new CancellationTokenSource();
    private CancellationTokenSource searchCancellation = new CancellationTokenSource();
    private NavigationService navigationService = null;
    private string hashCheckReport = string.Empty;
    private string hashValueStringOrChecklistPath = null;
    private JobStatus batchStatus = JobStatus.None;

    private RelayCommand mainWindowTopmostCmd;
    private RelayCommand clearAllTableLinesCmd;
    private RelayCommand exportHashResultsCmd;
    private RelayCommand copyAndRestartModelsCmd;
    private RelayCommand refreshOriginalModelsCmd;
    private RelayCommand forceRefreshOriginalModelsCmd;
    private RelayCommand selectChecklistFileCmd;
    private RelayCommand startCheckHashResultsCmd;
    private RelayCommand selectFilesToHashCmd;
    private RelayCommand selectFoldersToHashCmd;
    private RelayCommand cancelDisplayedModelsCmd;
    private RelayCommand pauseDisplayedModelsCmd;
    private RelayCommand continueDisplayedModelsCmd;
    private RelayCommand copyFilesNameCmd;
    private RelayCommand copyFilesFullPathCmd;
    private RelayCommand openFolderSelectItemsCmd;
    private RelayCommand openModelsFilePathCmd;
    private RelayCommand openFilesPropertyCmd;
    private RelayCommand deleteSelectedModelsFileCmd;
    private RelayCommand removeSelectedModelsCmd;
    private RelayCommand stopEnumeratingPackageCmd;
    private RelayCommand changeAlgosExportStateCmd;
    private RelayCommand copyModelsCurHashWithNoFormatCmd;
    private RelayCommand copyModelsAllHashWithNoFormatCmd;
    private RelayCommand displayMainWindowButtonsCmd;
    private RelayCommand openFilterOperationWindowCmd;

    private GenericItemModel[] copyModelsHashMenuCmds;
    private GenericItemModel[] copyModelsAllHashesMenuCmds;
    private GenericItemModel[] switchDisplayedAlgoCmds;
    private GenericItemModel[] switchAlgoExportStateCmds;
    private GenericItemModel[] ctrlHashViewModelTaskCmds;

    public HomeViewModel(FilterOperationModel model)
    {
        Current = this;
        this.FilterAndOperationModel = model;
        this.addModelAction = new Action<HashModelArg>(this.AddModelAction);
        this.addModelItemsAction = new Action<IEnumerable<HashModelArg>>(this.AddModelItemsAction);
        this.JobScheduler = new JobScheduler(Settings.Current.SelectedTaskNumberLimit);
        this.JobScheduler.JobStatusChanged += status =>
        {
            Synchronization.UI.Invoke(() => { this.BatchStatus = status; });
        };
        this.JobScheduler.PendingCountChanged += count =>
        {
            Synchronization.UI.Invoke(() => { this.PendingModelsCount = count; });
        };
    }

    public static HomeViewModel Current { get; private set; }

    public JobScheduler JobScheduler { get; }

    public FilterOperationWindow FilterWindowInstance { get; private set; }

    public FilterOperationModel FilterAndOperationModel { get; private set; }

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

    public object SelectedHashVieModel
    {
        get => this.selectedHashVieModel;
        set => this.SetPropNotify(ref this.selectedHashVieModel, value);
    }

    public string HashStringOrChecklistPath
    {
        get => this.hashValueStringOrChecklistPath;
        set => this.SetPropNotify(ref this.hashValueStringOrChecklistPath, value);
    }

    public JobStatus BatchStatus
    {
        get => this.batchStatus;
        set
        {
            if ((this.batchStatus != JobStatus.Started) && value == JobStatus.Started)
            {
                this.Report = string.Empty;
                this.SetPropNotify(ref this.batchStatus, value);
            }
            else if (this.batchStatus == JobStatus.Started && value == JobStatus.Stopped)
            {
                this.GenerateFileHashCheckReport();
                this.SetPropNotify(ref this.batchStatus, value);
            }
        }
    }

    public int PendingModelsCount
    {
        get => this.pendingModelsCount;
        set => this.SetPropNotify(ref this.pendingModelsCount, value);
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

    public void SetModelNavigationService(NavigationService service)
    {
        this.navigationService = service;
    }

    /// <summary>
    /// 检查剪贴板字符，在符合要求时把它设置到主窗口的校验信息输入框内
    /// </summary>
    public HashChecklist TestClipboardTextGetChecklist()
    {
        if (CommonUtils.ClipboardGetText(out string text) &&
            text.Length >= Settings.Current.MinCopiedCharsToTriggerHashCheck &&
            text.Length <= Settings.Current.MaxCopiedCharsToTriggerHashCheck)
        {
            HashChecklist checklist = File.Exists(text) ? HashChecklist.File(text) : HashChecklist.Text(text);
            if (checklist.ReasonForFailure == null)
            {
                this.HashStringOrChecklistPath = text;
                return checklist;
            }
        }
        return default(HashChecklist);
    }

    public void CheckHashUseClipboardText()
    {
        if (HashModelStore.HashViewModels.AnyItem() &&
            this.TestClipboardTextGetChecklist() is HashChecklist checklist)
        {
            if (this.BatchStatus != JobStatus.Started && this.CheckFilesHashBasedOnStringOrChecklist(checklist) &&
                Settings.Current.SwitchMainWndFgWhenNewHashCopied)
            {
                CommonUtils.ShowWindowForeground(MainWindow.ProcessId);
                // TODO: 实现跨进程导航至主页
            }
        }
    }

    private void AddModelAction(HashModelArg arg)
    {
        // 添加委托是同步投递到界面线程的，取消时已经投递但尚未执行的那一批无法撤销，
        // 若不在此拦截，取消/清空之后仍会添加新行并开始计算，表现为"取消后还有任务完成"
        if (this.stopAddingModels)
        {
            return;
        }
        HashViewModel model = new HashViewModel(++this.serial, arg);
        this.displayedModels.Add(model);
        HashModelStore.HashViewModels.Add(model);
        // 新添加的行状态为 NoState，直接启动即可满足启动条件
        if (Settings.Current.AutomaticallyStartTaskAfterFileAdded)
        {
            this.JobScheduler.Start(model, force: false);
        }
    }

    private void AddModelItemsAction(IEnumerable<HashModelArg> args)
    {
        // 添加委托是同步投递到界面线程的，取消时已经投递但尚未执行的那一批无法撤销，
        // 若不在此拦截，取消/清空之后仍会添加新行并开始计算，表现为"取消后还有任务完成"
        if (this.stopAddingModels)
        {
            return;
        }
        List<HashViewModel> preprocessedModels = new();
        bool automaticallyStartTaskAfterFileAdded =
            Settings.Current.AutomaticallyStartTaskAfterFileAdded;
        foreach (HashModelArg arg in args)
        {
            HashViewModel model = new HashViewModel(++this.serial, arg);
            preprocessedModels.Add(model);
            if (automaticallyStartTaskAfterFileAdded)
            {
                // 新添加的行状态为 NoState，直接启动即可满足启动条件
                this.JobScheduler.Start(model, force: false);
            }
        }
        this.displayedModels.AddRange(preprocessedModels);
        HashModelStore.HashViewModels.AddItems(preprocessedModels);
    }

    public async void BeginDisplayModels(IEnumerable<HashViewModel> models)
    {
        this.stopAddingModels = false;
        CancellationToken token = this.Cancellation.Token;
        await Task.Run(() =>
        {
            lock (this.displayingModelLock)
            {
                int batchSize = Settings.Current.AddHashViewModelsBatchSize;
                if (batchSize <= 1)
                {
                    foreach (HashModelArg arg in models.Select(i => i.Arguments))
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        arg.PresetAlgos = null;
                        Synchronization.UI.Invoke(this.addModelAction,
                            DispatcherPriority.Background, arg);
                    }
                }
                else
                {
                    List<HashModelArg> buffer = new(batchSize);
                    foreach (HashModelArg arg in models.Select(i => i.Arguments))
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        arg.PresetAlgos = null;
                        buffer.Add(arg);
                        if (buffer.Count >= batchSize)
                        {
                            Synchronization.UI.Invoke(this.addModelItemsAction,
                                DispatcherPriority.Background, buffer);
                            buffer.Clear();
                        }
                    }
                    if (buffer.Count > 0)
                    {
                        Synchronization.UI.Invoke(this.addModelItemsAction,
                            DispatcherPriority.Background, buffer);
                    }
                }
            }
        }, token);
    }

    public async void BeginDisplayModels(params PathPackage[] packages)
    {
        this.stopAddingModels = false;
        CancellationToken token = this.Cancellation.Token;
        await Task.Run(() =>
        {
            CancellationToken stopSearchingToken = this.searchCancellation.Token;
            lock (this.displayingModelLock)
            {
                int batchSize = Settings.Current.AddHashViewModelsBatchSize;
                if (batchSize <= 1)
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
                            Synchronization.UI.Invoke(this.addModelAction,
                                DispatcherPriority.Background, arg);
                        }
                    }
                }
                else
                {
                    List<HashModelArg> buffer = new(batchSize);
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
                            buffer.Add(arg);
                            if (buffer.Count >= batchSize)
                            {
                                Synchronization.UI.Invoke(this.addModelItemsAction,
                                    DispatcherPriority.Background, buffer);
                                buffer.Clear();
                            }
                        }
                    }
                    if (buffer.Count > 0)
                    {
                        Synchronization.UI.Invoke(this.addModelItemsAction,
                            DispatcherPriority.Background, buffer);
                    }
                }
            }
        }, token);
    }

    public void GenerateFileHashCheckReport()
    {
        if (Settings.Current.ClearSelectedItemsAfterCompletion)
        {
            this.SelectedHashVieModel = null;
        }
        int noresult, unrelated, matched, mismatch,
            uncertain, succeeded, canceled, hasFailed, totalHash;
        noresult = unrelated = matched = mismatch =
            uncertain = succeeded = canceled = hasFailed = totalHash = 0;
        Dictionary<byte[], List<HashViewModel>> hashViewModels =
            new Dictionary<byte[], List<HashViewModel>>(BytesComparer.Default);
        foreach (HashViewModel hm in HashModelStore.HashViewModels)
        {
            hm.HashColorID = null;
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
                    if (Settings.Current.MarkTheSameHashValues)
                    {
                        byte[] key = hm.CurrentInOutModel.HashResult;
                        if (!hashViewModels.TryGetValue(key, out List<HashViewModel> value))
                        {
                            value = new List<HashViewModel>();
                            hashViewModels[key] = value;
                        }
                        value.Add(hm);
                    }
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
        builder.Append($"总行数：{HashModelStore.HashViewModels.Count}\n已成功：{succeeded}\n");
        builder.Append($"已失败：{hasFailed}\n已取消：{canceled}\n\n");
        builder.Append($"校验汇总：\n算法数：{totalHash}\n");
        builder.Append($"已匹配：{matched}\n不匹配：{mismatch}\n");
        builder.Append($"不确定：{uncertain}\n无关联：{unrelated}\n未校验：{noresult}");
        this.Report = builder.ToString();
        if (Settings.Current.MarkTheSameHashValues)
        {
            KeyValuePair<byte[], List<HashViewModel>>[] finalModels = hashViewModels.Where(
                i => i.Value.Count > 1).ToArray();
            IEnumerable<ComparableColor> colors = CommonUtils.ColorGenerator(
                finalModels.Length,
                Settings.Current.LuminanceOfTableRowsWithSameHash,
                Settings.Current.SaturationOfTableRowsWithSameHash).Select(i => new ComparableColor(i));
            foreach (Tuple<KeyValuePair<byte[], List<HashViewModel>>, ComparableColor> tuple in finalModels.ZipElements(colors))
            {
                foreach (HashViewModel hashViewModel in tuple.Item1.Value)
                {
                    hashViewModel.HashColorID = tuple.Item2;
                }
            }
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
                NotificationSender.SnackbarSuccess($"已按模板复制所选行的当前结果或全部结果");
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
            this.copyModelsHashMenuCmds ??= new GenericItemModel[]
                {
                    new GenericItemModel("Base64 格式", new RelayCommand(this.CopyModelsHashBase64Action)),
                    new GenericItemModel("十六进制大写", new RelayCommand(this.CopyModelsHashBinUpperAction)),
                    new GenericItemModel("十六进制小写", new RelayCommand(this.CopyModelsHashBinLowerAction)),
                };
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
            this.copyModelsAllHashesMenuCmds ??= new GenericItemModel[]
                {
                    new GenericItemModel("Base64 格式", new RelayCommand(this.CopyModelsAllBase64HashesAction)),
                    new GenericItemModel("十六进制大写", new RelayCommand(this.CopyModelsAllBinUpperHashesAction)),
                    new GenericItemModel("十六进制小写", new RelayCommand(this.CopyModelsAllBinLowerHashesAction)),
                };
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
                NotificationSender.SnackbarSuccess("已复制文件名或文件路径到剪贴板。");
            }
            if (!copyName && !fullPathCopied)
            {
                NotificationSender.SnackbarWarning("文件不存在所以完整路径没有被复制。");
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
            this.copyFilesNameCmd ??= new RelayCommand(this.CopyFilesNameAction);
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
            this.copyFilesFullPathCmd ??= new RelayCommand(this.CopyFilesPathAction);
            return this.copyFilesFullPathCmd;
        }
    }

    private void CopyModelsHashWithNoFormatAction(object param, OutputType outputType, bool copyAll)
    {
        if (param is IList selectedModels && selectedModels.AnyItem())
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (HashViewModel model in selectedModels)
            {
                if (model.GenerateTextInFormat(null, outputType, copyAll, endLine: true, seeExport: false,
                    casedName: false) is string text)
                {
                    stringBuilder.Append(text);
                }
            }
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                CommonUtils.ClipboardSetText(stringBuilder.ToString());
                NotificationSender.SnackbarSuccess("已复制所选行的当前哈希值或全部哈希值");
            }
        }
    }

    private void CopyModelsCurHashWithNoFormatAction(object param)
    {
        this.CopyModelsHashWithNoFormatAction(param, OutputType.Unknown, copyAll: false);
    }

    public ICommand CopyModelsCurHashWithNoFormatCmd
    {
        get
        {
            this.copyModelsCurHashWithNoFormatCmd ??= new RelayCommand(this.CopyModelsCurHashWithNoFormatAction);
            return this.copyModelsCurHashWithNoFormatCmd;
        }
    }

    private void CopyModelsAllHashWithNoFormatAction(object param)
    {
        this.CopyModelsHashWithNoFormatAction(param, OutputType.Unknown, copyAll: true);
    }

    public ICommand CopyModelsAllHashWithNoFormatCmd
    {
        get
        {
            this.copyModelsAllHashWithNoFormatCmd ??= new RelayCommand(this.CopyModelsAllHashWithNoFormatAction);
            return this.copyModelsAllHashWithNoFormatCmd;
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
            if (groupByDir.Count != 0)
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
            this.openFolderSelectItemsCmd ??= new RelayCommand(this.OpenFolderSelectItemsAction);
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
                HashViewModel model = (HashViewModel)selectedModels[i];
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
            this.openModelsFilePathCmd ??= new RelayCommand(this.OpenModelsFilePathAction);
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
                    SHELLEXECUTEINFOW shellExecuteInfo = new SHELLEXECUTEINFOW();
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
            this.openFilesPropertyCmd ??= new RelayCommand(this.OpenFilesPropertyAction);
            return this.openFilesPropertyCmd;
        }
    }

    private void DisplayMainWindowButtonsAction(object param)
    {
        Settings.Current.DisplayMainWindowButtons = true;
    }

    public ICommand DisplayMainWindowButtonsCmd
    {
        get
        {
            this.displayMainWindowButtonsCmd ??= new RelayCommand(this.DisplayMainWindowButtonsAction);
            return this.displayMainWindowButtonsCmd;
        }
    }

    private void OpenFilterOperationWindowAction(object param)
    {
        if (this.FilterWindowInstance is null)
        {
            this.FilterWindowInstance = App.GetRequiredService<FilterOperationWindow>();
            this.FilterWindowInstance.Closed += (s, e) => { this.FilterWindowInstance = null; };
            this.FilterWindowInstance.Show();
        }
        else
        {
            if (this.FilterWindowInstance.WindowState != WindowState.Normal)
            {
                this.FilterWindowInstance.WindowState = WindowState.Normal;
            }
            if (!this.FilterWindowInstance.IsActive)
            {
                this.FilterWindowInstance.Activate();
            }
        }
    }

    public ICommand OpenFilterOperationWindowCmd
    {
        get
        {
            this.openFilterOperationWindowCmd ??= new RelayCommand(this.OpenFilterOperationWindowAction);
            return this.openFilterOperationWindowCmd;
        }
    }

    public void RefreshAllOutputTypeAction()
    {
        foreach (HashViewModel model in HashModelStore.HashViewModels)
        {
            if (model.HasBeenRun)
            {
                model.SelectedOutputType = Settings.Current.SelectedOutputType;
            }
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
            if (NotificationSender.ShowMessageBox(
                MainWindow.Current,
                "提示",
                deleteFileTip,
                closeButtonText: "否",
                primaryButtonText: "是") != Wpfctrls.ContentDialogResult.Primary)
            {
                return;
            }
            ProgressWindowModel progress = new ProgressWindowModel()
            {
                IsCancelled = true,
                TotalCount = count,
                SubProgressVisibility = Visibility.Collapsed,
                TotalProgressVisibility = Visibility.Collapsed,
                WindowTitle = "正在删除...",
                TotalString = "文件数量多的情况下耗时较长，请耐心等候...",
            };
            ProgressWindow progressWindow = new ProgressWindow(progress)
            {
                Owner = MainWindow.Current,
            };
            HashViewModel[] targets = selectedModels.Cast<HashViewModel>().ToArray();
            this.JobScheduler.Cancel(targets);
            HashModelStore.HashViewModels.RemoveItems(targets);
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
                        if (fileNameList.Count != 0)
                        {
                            return "以下文件删除失败：\n" + '\n'.Join(fileNameList);
                        }
                        return default(string);
                    }
                    else
                    {
                        string allInOneStr = '\0'.Join(targets.Select(i => i.Information.FullName));
                        if (!CommonUtils.SendToRecycleBin(MainWindow.WndHandle,
                            allInOneStr, Settings.Current.MoveFilesToRecycleBinSilently))
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
                    Synchronization.UI.Invoke(() => { progressWindow.DialogResult = false; });
                }
            });
            progressWindow.ShowDialog();
            string exceptionMessage = await deleteFileTask;
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                NotificationSender.ShowMessageBox(MainWindow.Current, "错误", exceptionMessage);
            }
            this.GenerateFileHashCheckReport();
        }
    }

    public ICommand DeleteSelectedModelsFileCmd
    {
        get
        {
            this.deleteSelectedModelsFileCmd ??= new RelayCommand(this.DeleteSelectedModelsFileAction);
            return this.deleteSelectedModelsFileCmd;
        }
    }

    private void RemoveSelectedModelsAction(object param)
    {
        if (param is IList selectedModels)
        {
            HashViewModel[] models = selectedModels.Cast<HashViewModel>().ToArray();
            this.JobScheduler.Cancel(models);
            foreach (HashViewModel model in models)
            {
                this.displayedModels.Remove(model);
            }
            HashModelStore.HashViewModels.RemoveItems(models);
            this.GenerateFileHashCheckReport();
        }
    }

    public ICommand RemoveSelectedModelsCmd
    {
        get
        {
            this.removeSelectedModelsCmd ??= new RelayCommand(this.RemoveSelectedModelsAction);
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
            this.mainWindowTopmostCmd ??= new RelayCommand(this.MainWindowTopmostAction);
            return this.mainWindowTopmostCmd;
        }
    }

    public void ClearAllTableLinesAction(object param)
    {
        this.CancelDisplayedModelsAction(null);
        this.serial = 0;
        this.displayedModels.Clear();
        HashModelStore.HashViewModels.Clear();
    }

    public ICommand ClearAllTableLinesCmd
    {
        get
        {
            this.clearAllTableLinesCmd ??= new RelayCommand(this.ClearAllTableLinesAction);
            return this.clearAllTableLinesCmd;
        }
    }

    private void ExporHashResultAction(object param)
    {
        if (!HashModelStore.HashViewModels.Any(i => i.Result == HashResult.Succeeded))
        {
            NotificationSender.SnackbarSecondary("主页列表中没有可以导出的结果。");
            return;
        }
        if (Settings.Current.TemplatesForExport?.Any() != true)
        {
            NotificationSender.SnackbarWarning("没有可用的导出方案，请到【导出行为】中添加。");
            return;
        }
        if (Settings.Current.AskUserHowToExportResultsEveryTime)
        {
            HowToExportResults howToExportResults = new HowToExportResults()
            {
                Owner = MainWindow.Current,
            };
            if (howToExportResults.ShowDialog() != true)
            {
                return;
            }
        }
        List<TemplateForExportModel> usedModels = new List<TemplateForExportModel>();
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
        if (usedModels.Count == 0)
        {
            NotificationSender.SnackbarWarning(
                "没有可用方案，可能方案的扩展名中存在不能用作文件名的字符，请到【导出行为】中修改。");
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
            NotificationSender.SnackbarError($"导出哈希值失败，异常信息：{ex.Message}");
        }
    }

    private void EachAlgoExportedToSeparateFile(string file, Encoding encoding, string format,
        OutputType output)
    {
        Dictionary<AlgoType, string> algoTypes = new Dictionary<AlgoType, string>();
        List<HashViewModel> validHashViews = new List<HashViewModel>();
        foreach (HashViewModel hashView in HashModelStore.HashViewModels)
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
        if (existedFiles.Count != 0)
        {
            string paths = '\n'.Join(existedFiles);
            if (NotificationSender.ShowMessageBox(
                MainWindow.Current,
                "警告",
                $"已存在以下文件，继续导出将会覆盖原文件，仍然要导出吗？\n{paths}",
                closeButtonText: "否",
                primaryButtonText: "是") != Wpfctrls.ContentDialogResult.Primary)
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
                if (NotificationSender.ShowMessageBox(
                    MainWindow.Current,
                    "警告",
                    "并非所有行包含的算法都一样，如果仍要导出结果，则导出的每个清单里包含的文件数量不一样，" +
                        "仍然要导出吗？",
                    closeButtonText: "否",
                    primaryButtonText: "是") == Wpfctrls.ContentDialogResult.Primary)
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

    private void AlgorithmResultsExportedToSameFile(string file, Encoding encoding,
        string format, OutputType output, bool all)
    {
        using (FileStream fileStream = File.Create(file))
        using (StreamWriter streamWriter = new StreamWriter(fileStream, encoding))
        {
            foreach (HashViewModel hm in HashModelStore.HashViewModels
                .Where(i => i.Result == HashResult.Succeeded))
            {
                if (hm.GenerateTextInFormat(format, output, all, endLine: true, seeExport: true,
                    casedName: false) is string text)
                {
                    streamWriter.Write(text);
                }
            }
        }
    }

    public ICommand ExportHashResultsCmd
    {
        get
        {
            this.exportHashResultsCmd ??= new RelayCommand(this.ExporHashResultAction);
            return this.exportHashResultsCmd;
        }
    }

    public void StartModels(bool newLines, bool force)
    {
        if (!newLines)
        {
            this.JobScheduler.Start(HashModelStore.HashViewModels, force);
        }
        else if (this.displayedModels.Count != 0)
        {
            List<HashViewModel> args = this.displayedModels;
            this.displayedModels = new List<HashViewModel>();
            this.BeginDisplayModels(args);
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
            this.copyAndRestartModelsCmd ??= new RelayCommand(this.CopyAndRestartModelsAction);
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
            this.refreshOriginalModelsCmd ??= new RelayCommand(this.RefreshOriginalModelsAction);
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
            this.forceRefreshOriginalModelsCmd ??= new RelayCommand(this.ForceRefreshOriginalModelsAction);
            return this.forceRefreshOriginalModelsCmd;
        }
    }

    private void GenerateOriginFileHashCheckReport()
    {
        foreach (HashViewModel hm in HashModelStore.HashViewModels)
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
                NotificationSender.SnackbarWarning("校验信息输入框没有任何内容！");
                return false;
            }
            // HashStringOrChecklistPath 不是一个文件
            if (!File.Exists(this.HashStringOrChecklistPath))
            {
                localChecklist = HashChecklist.Text(this.HashStringOrChecklistPath);
            }
            // HashStringOrChecklistPath 是一个文件，但哈希结果列表不是空
            else if (HashModelStore.HashViewModels.Any())
            {
                localChecklist = HashChecklist.File(this.HashStringOrChecklistPath);
                try
                {
                    string parent = Path.GetDirectoryName(this.HashStringOrChecklistPath);
                    // TODO: 统一 AssertRelativeGetFull 方法的调用方式
                    // 目前的情况是在此处有调用，在 PathPackage 内也有调用
                    localChecklist.AssertRelativeGetFull(parent, out _);
                }
                catch (Exception)
                {
                    NotificationSender.SnackbarWarning("无法获取校验信息文件的目录信息。");
                    return false;
                }
            }
            // HashStringOrChecklistPath 是一个文件，且哈希结果列表也是空
            else
            {
                HashChecklist newChecklist = HashChecklist.File(this.HashStringOrChecklistPath);
                if (newChecklist.ReasonForFailure != null)
                {
                    NotificationSender.SnackbarError(newChecklist.ReasonForFailure);
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
            foreach (HashViewModel hm in HashModelStore.HashViewModels)
            {
                hm.SetHashCheckResultForModel(localChecklist);
            }
            this.GenerateFileHashCheckReport();
        }
        else
        {
            this.GenerateOriginFileHashCheckReport();
            NotificationSender.SnackbarError(localChecklist.ReasonForFailure);
        }
        return localChecklist.ReasonForFailure == null;
    }

    private void SelectChecklistFileAction(object param)
    {
        OpenFileDialog openFile = new OpenFileDialog
        {
            Title = "选择校验信息文件",
            InitialDirectory = Settings.Current.LastUsedPath,
            CheckFileExists = true,
            CheckPathExists = true,
        };
        if (openFile.ShowDialog() == true)
        {
            Settings.Current.LastUsedPath = Path.GetDirectoryName(openFile.FileName);
            this.HashStringOrChecklistPath = openFile.FileName;
        }
    }

    public ICommand SelectChecklistFileCmd
    {
        get
        {
            this.selectChecklistFileCmd ??= new RelayCommand(this.SelectChecklistFileAction);
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
            this.startCheckHashResultsCmd ??= new RelayCommand(this.StartCheckHashResultsAction);
            return this.startCheckHashResultsCmd;
        }
    }

    private void SelectFilesToHashAction(object param)
    {
        OpenFileDialog openFiles = new OpenFileDialog
        {
            Title = "选择文件",
            InitialDirectory = Settings.Current.LastUsedPath,
            Multiselect = true,
        };
        if (openFiles.ShowDialog() != true)
        {
            return;
        }
        string parentDir = Path.GetDirectoryName(openFiles.FileNames.First());
        Settings.Current.LastUsedPath = parentDir;
        this.BeginDisplayModels(new PathPackage(parentDir, openFiles.FileNames,
            Settings.Current.SelectedSearchMethodForDragDrop));
    }

    public ICommand SelectFilesToHashCmd
    {
        get
        {
            this.selectFilesToHashCmd ??= new RelayCommand(this.SelectFilesToHashAction);
            return this.selectFilesToHashCmd;
        }
    }

    private void SelectFolderToHashAction(object param)
    {
        OpenFolderDialog openFolders = new OpenFolderDialog()
        {
            InitialDirectory = Settings.Current.LastUsedPath,
            Multiselect = true,
        };
        if (openFolders.ShowDialog() != true)
        {
            return;
        }
        SearchMethod searchMethod = Settings.Current.SelectedSearchMethodForDragDrop;
        if (searchMethod == SearchMethod.DontSearch)
        {
            searchMethod = SearchMethod.Children;
        }
        string firstDir = openFolders.FolderNames.First();
        // firstDir 是分区根目录时 GetDirectoryName 返回 null
        string parentDir = Path.GetDirectoryName(firstDir) ?? firstDir;
        Settings.Current.LastUsedPath = parentDir;
        this.BeginDisplayModels(new PathPackage(parentDir, openFolders.FolderNames, searchMethod));
    }

    public ICommand SelectFoldersToHashCmd
    {
        get
        {
            this.selectFoldersToHashCmd ??= new RelayCommand(this.SelectFolderToHashAction);
            return this.selectFoldersToHashCmd;
        }
    }

    private void CancelDisplayedModelsAction(object param)
    {
        // Cancellation 用于终止正在向表格添加行的搜索过程，与计算无关，故不交给调度器处理
        // 先置标志再取消：界面线程可能已排入尚未执行的添加委托，
        // 它们会在本方法返回之后才被处理，必须让它们整批跳过
        this.stopAddingModels = true;
        this.Cancellation?.Cancel();
        this.JobScheduler.CancelAll();
        // 从未启动的作业不在调度器的作业集合内，须在此单独终结，
        // 本按钮承诺的范围包含"未开始"的作业
        foreach (HashViewModel model in HashModelStore.HashViewModels)
        {
            model.MarkCanceled();
        }
        this.Cancellation?.Dispose();
        this.Cancellation = new CancellationTokenSource();
    }

    public ICommand CancelDisplayedModelsCmd
    {
        get
        {
            this.cancelDisplayedModelsCmd ??= new RelayCommand(this.CancelDisplayedModelsAction);
            return this.cancelDisplayedModelsCmd;
        }
    }

    private void PauseDisplayedModelsAction(object param)
    {
        this.JobScheduler.PauseAll();
    }

    public ICommand PauseDisplayedModelsCmd
    {
        get
        {
            this.pauseDisplayedModelsCmd ??= new RelayCommand(this.PauseDisplayedModelsAction);
            return this.pauseDisplayedModelsCmd;
        }
    }

    private void ContinueDisplayedModelsAction(object param)
    {
        // 已暂停的由调度器唤醒，从未启动的在此启动。
        // 已结束的（含被取消的）留白给【计算缺值项】，本按钮不启动它们
        this.JobScheduler.ResumeAll();
        this.JobScheduler.Start(
            HashModelStore.HashViewModels.Where(i => i.State == HashState.NoState),
            force: false);
    }

    public ICommand ContinueDisplayedModelsCmd
    {
        get
        {
            this.continueDisplayedModelsCmd ??= new RelayCommand(this.ContinueDisplayedModelsAction);
            return this.continueDisplayedModelsCmd;
        }
    }

    private void PauseSelectedModelsAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.JobScheduler.Pause(selectedModels.Cast<HashViewModel>());
        }
    }

    private void CancelSelectedModelsAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.JobScheduler.Cancel(selectedModels.Cast<HashViewModel>());
        }
    }

    private void ContinueSelectedModelsAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.JobScheduler.Resume(selectedModels.Cast<HashViewModel>());
        }
    }

    private void RestartSelectedModelsForceAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.JobScheduler.Start(selectedModels.Cast<HashViewModel>(), force: true);
        }
    }

    private void RestartSelectedUnsucceededModelsAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.JobScheduler.Start(selectedModels.Cast<HashViewModel>(), force: false);
        }
    }

    private void RestartSelectedModelsNewLineAction(object param)
    {
        if (param is IList selectedModels)
        {
            this.BeginDisplayModels(selectedModels.Cast<HashViewModel>());
        }
    }

    public GenericItemModel[] CtrlHashViewModelTaskCmds
    {
        get
        {
            this.ctrlHashViewModelTaskCmds ??= new GenericItemModel[] {
                    new GenericItemModel("暂停任务", new RelayCommand(this.PauseSelectedModelsAction)),
                    new GenericItemModel("继续任务", new RelayCommand(this.ContinueSelectedModelsAction)),
                    new GenericItemModel("取消任务", new RelayCommand(this.CancelSelectedModelsAction)),
                    new GenericItemModel("新行重算", new RelayCommand(this.RestartSelectedModelsNewLineAction)),
                    new GenericItemModel("计算缺值项", new RelayCommand(this.RestartSelectedUnsucceededModelsAction)),
                    new GenericItemModel("重新计算", new RelayCommand(this.RestartSelectedModelsForceAction)),
                };
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
            this.stopEnumeratingPackageCmd ??= new RelayCommand(this.StopEnumeratingPackageAction);
            return this.stopEnumeratingPackageCmd;
        }
    }

    /// <summary>
    /// 改变所选行的当前导出状态
    /// </summary>
    private void ChangeCurExportState(object param, bool export)
    {
        if (param is IList selectedModels)
        {
            foreach (HashViewModel model in selectedModels)
            {
                model.CurrentInOutModel?.Export = export;
            }
        }
    }

    /// <summary>
    /// 改变所选行的所有导出状态
    /// </summary>
    private void ChangeAllExportState(object param, bool export)
    {
        if (param is IList selectedModels)
        {
            foreach (HashViewModel model in selectedModels)
            {
                if (model.AlgoInOutModels != null)
                {
                    foreach (AlgoInOutModel inOutModel in model.AlgoInOutModels)
                    {
                        inOutModel.Export = export;
                    }
                }
            }
        }
    }

    public GenericItemModel[] SwitchAlgoExportStateCmds
    {
        get
        {
            this.switchAlgoExportStateCmds ??= new GenericItemModel[]
                {
                    new GenericItemModel(
                        "启用所选行当前导出状态",
                        new RelayCommand(obj => this.ChangeCurExportState(obj, true))),
                    new GenericItemModel(
                        "取消所选行当前导出状态",
                        new RelayCommand(obj => this.ChangeCurExportState(obj, false))),
                    new GenericItemModel(
                        "启用所选行所有导出状态",
                        new RelayCommand(obj => this.ChangeAllExportState(obj, true))),
                    new GenericItemModel(
                        "取消所选行所有导出状态",
                        new RelayCommand(obj => this.ChangeAllExportState(obj, false))),
                };
            return this.switchAlgoExportStateCmds;
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
                this.switchDisplayedAlgoCmds = AlgorithmsModel.ProvidedAlgos.Select(
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
            this.changeAlgosExportStateCmd ??= new RelayCommand(this.ChangeAlgosExportStateAction);
            return this.changeAlgosExportStateCmd;
        }
    }
}
