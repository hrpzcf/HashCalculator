using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Windows;

namespace HashCalculator;

public class HashViewModel : BaseViewModel
{
    private string _fileName = string.Empty;
    private string _currentHashString = null;
    private string _errorDetails = "任务未开始...";
    private long _fileLength = 0L;
    private long _progress = 0L;
    private long _maxProgress = 0L;
    private double _durationofTask = double.NaN;
    private bool _isExecutionTarget = false;
    private HcmData _hcmDataFromFile = null;
    private AlgoInOutModel _currentInOutModel = null;
    private ObservableCollection<AlgoInOutModel> _algoInOutModels = null;
    private ComparableColor _hashColorID = null;
    private ComparableColor _hashGroupId = null;
    private ComparableColor _embeddedHashGroupId = null;
    private ComparableColor _folderGroupId = null;
    private HashState _currentState = HashState.NoState;
    private HashResult _currentResult = HashResult.NoResult;
    private OutputType _selectedOutput = OutputType.Unknown;
    private RelayCommand shutdownModelSelfCmd;
    private RelayCommand restartModelSelfCmd;
    private RelayCommand pauseOrResumeModelSelfCmd;
    private RelayCommand copyThisModelCurHashCmd;
    private RelayCommand copyThisModelAllHashesCmd;
    private RelayCommand tableColumnDoubleClickCmd;

    private static readonly Dispatcher synchronization =
        Application.Current.Dispatcher;
    private readonly ManualResetEvent manualPauseController =
        new ManualResetEvent(true);
    private readonly object computeHashOperationLock = new object();
    private CancellationTokenSource cancellation;
    /// <summary>
    /// 调度器期望本模型处于的状态。<br/>
    /// 调度线程与界面线程都会改写 State，若各自按自己捕获的值写入，先发生的界面状态变更
    /// 会被后执行的异步投影覆盖：作业已被暂停时，此前排入队列的"运行中"投影随后执行，
    /// 又把状态改回运行中。故统一在此记录最新期望值，异步投影时读取本字段而非捕获值，
    /// 保证最终显示的是最后一次的状态决定。
    /// </summary>
    private volatile HashState desiredState = HashState.NoState;

    public HashViewModel(int serial, HashModelArg arg)
    {
        this.Arguments = arg;
        this.SerialNumber = serial;
        this.FileName = arg.FileName;
        this.Information = new FileInfo(arg.FilePath);
        try
        {
            if (!arg.Deprecated)
            {
                this.FileLength = this.Information.Length;
                this.FileIcon = CommonUtils.GetFileIcon(arg.FilePath, true);
            }
            else
            {
                this.FileLength = -1;
            }
        }
        catch (Exception e) when (e is IOException || e is FileNotFoundException)
        {
            this.FileLength = -1;
        }
        this.RelativePath = arg.FileRelativePath;
        this.InvalidFileName = arg.IsInvalidName;
        if (arg.PresetAlgos != null)
        {
            this.AlgoInOutModels = AlgorithmsModel.GetKnownAlgos(arg.PresetAlgos);
        }
        else if (Settings.Current.PreferChecklistAlgs && arg.HashChecklist != null)
        {
            this.AlgoInOutModels = AlgorithmsModel.GetAlgsFromChecklist(arg.HashChecklist,
                this.RelativePath);
        }
        this.PropertyChanged += this.CurrentHashStringHandler;
    }

    public int SerialNumber { get; }

    public FileInfo Information { get; }

    public BitmapSource FileIcon { get; }

    public string RelativePath { get; }

    public bool InvalidFileName { get; }

    public HashModelArg Arguments { get; }

    public CmpableFileIndex FileIndex { get; set; }

    public bool Matched { get; set; } = true;

    public bool HasBeenRun { get; private set; }

    public string FileName
    {
        get => this._fileName;
        set => this.SetPropNotify(ref this._fileName, value);
    }

    public long FileLength
    {
        get => this._fileLength;
        set => this.SetPropNotify(ref this._fileLength, value);
    }

    public string CurrentHashString
    {
        get => this._currentHashString;
        set => this.SetPropNotify(ref this._currentHashString, value);
    }

    public AlgoInOutModel CurrentInOutModel
    {
        get => this._currentInOutModel;
        set => this.SetPropNotify(ref this._currentInOutModel, value);
    }

    public HcmData HcmDataFromFile
    {
        get => this._hcmDataFromFile;
        set => this.SetPropNotify(ref this._hcmDataFromFile, value);
    }

    public ComparableColor HashColorID
    {
        get => this._hashColorID;
        set => this.SetPropNotify(ref this._hashColorID, value);
    }

    /// <summary>
    /// 相同哈希值分组标识
    /// </summary>
    public ComparableColor HashGroupID
    {
        get => this._hashGroupId;
        set => this.SetPropNotify(ref this._hashGroupId, value);
    }

    /// <summary>
    /// 相同的内嵌哈希值分组标识
    /// </summary>
    public ComparableColor EmbeddedHashGroupID
    {
        get => this._embeddedHashGroupId;
        set => this.SetPropNotify(ref this._embeddedHashGroupId, value);
    }

    /// <summary>
    /// 相同文件夹分组标识
    /// </summary>
    public ComparableColor FolderGroupID
    {
        get => this._folderGroupId;
        set => this.SetPropNotify(ref this._folderGroupId, value);
    }

    public ObservableCollection<AlgoInOutModel> AlgoInOutModels
    {
        get => this._algoInOutModels;
        set => this.SetPropNotify(ref this._algoInOutModels, value);
    }

    public HashState State
    {
        get => this._currentState;
        private set
        {
            this.SetPropNotify(ref this._currentState, value);
            if (value == HashState.NoState)
            {
                this.ErrorDetails = "任务未开始...";
            }
            else if (value == HashState.Waiting)
            {
                this.ErrorDetails = "任务排队中...";
            }
        }
    }

    public HashResult Result
    {
        get => this._currentResult;
        private set
        {
            this.SetPropNotify(ref this._currentResult, value);
            if (value == HashResult.Canceled)
            {
                this.ErrorDetails = "任务已取消...";
            }
        }
    }

    public long Progress
    {
        get => this._progress;
        set => this.SetPropNotify(ref this._progress, value);
    }

    public long MaxProgress
    {
        get => this._maxProgress;
        set => this.SetPropNotify(ref this._maxProgress, value);
    }

    public string ErrorDetails
    {
        get => this._errorDetails;
        set => this.SetPropNotify(ref this._errorDetails, value);
    }

    public double DurationofTask
    {
        get => this._durationofTask;
        set => this.SetPropNotify(ref this._durationofTask, value);
    }

    public bool IsExecutionTarget
    {
        get => this._isExecutionTarget;
        set => this.SetPropNotify(ref this._isExecutionTarget, value);
    }

    // Xaml 绑定会更改此值，不使用 private set
    public OutputType SelectedOutputType
    {
        get => this._selectedOutput;
        set => this.SetPropNotify(ref this._selectedOutput, value);
    }

    private void CopyThisModelCurHashAction(object param)
    {
        string format = Settings.Current.GenerateTextInFormat ?
            Settings.Current.FormatForGenerateText : null;
        if (this.GenerateTextInFormat(
            format, this.SelectedOutputType, all: false, endLine: false, seeExport: false,
            Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
        {
            CommonUtils.ClipboardSetText(text);
            NotificationSender.SnackbarSuccess($"已按模板复制当前哈希值：\n{text}");
        }
    }

    public ICommand CopyThisModelCurHashCmd
    {
        get
        {
            this.copyThisModelCurHashCmd ??= new RelayCommand(this.CopyThisModelCurHashAction);
            return this.copyThisModelCurHashCmd;
        }
    }

    private void CopyThisModelAllHashesAction(object param)
    {
        string format = Settings.Current.GenerateTextInFormat ?
            Settings.Current.FormatForGenerateText : null;
        if (this.GenerateTextInFormat(
            format, this.SelectedOutputType, all: true, endLine: false, seeExport: false,
            Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
        {
            CommonUtils.ClipboardSetText(text);
            NotificationSender.SnackbarSuccess($"已按模板复制所有哈希值：\n{text}");
        }
    }

    public ICommand CopyThisModelAllHashesCmd
    {
        get
        {
            this.copyThisModelAllHashesCmd ??= new RelayCommand(this.CopyThisModelAllHashesAction);
            return this.copyThisModelAllHashesCmd;
        }
    }

    private void ShutdownModelSelfAction(object param)
    {
        JobScheduler.Current?.Cancel(this);
    }

    public ICommand ShutdownModelSelfCmd
    {
        get
        {
            this.shutdownModelSelfCmd ??= new RelayCommand(this.ShutdownModelSelfAction);
            return this.shutdownModelSelfCmd;
        }
    }

    private void RestartModelSelfAction(object param)
    {
        JobScheduler.Current?.Start(this, force: false);
    }

    public ICommand RestartModelSelfCmd
    {
        get
        {
            this.restartModelSelfCmd ??= new RelayCommand(this.RestartModelSelfAction);
            return this.restartModelSelfCmd;
        }
    }

    private void PauseOrResumeModelSelfAction(object param)
    {
        JobScheduler.Current?.PauseOrResume(this);
    }

    public ICommand PauseOrResumeModelSelfCmd
    {
        get
        {
            this.pauseOrResumeModelSelfCmd ??= new RelayCommand(this.PauseOrResumeModelSelfAction);
            return this.pauseOrResumeModelSelfCmd;
        }
    }

    public void ShowHashDetailsWindowAction()
    {
        new HashDetailsWindow(this) { Owner = MainWindow.Current }.ShowDialog();
    }

    private void TableColumnDoubleClickAction(object param)
    {
        if (param is string commandString && !string.IsNullOrEmpty(commandString))
        {
            switch (commandString)
            {
                case SettingsViewModel.CmdStrShowDetails:
                    if (this.Result == HashResult.Succeeded)
                    {
                        this.ShowHashDetailsWindowAction();
                    }
                    else
                    {
                        NotificationSender.SnackbarWarning("没有完成哈希值计算！");
                    }
                    break;
                case SettingsViewModel.CmdStrOpenFile:
                    if (File.Exists(this.Information.FullName))
                    {
                        SHELL32.ShellExecuteW(MainWindow.WndHandle, "open",
                            this.Information.FullName, null, this.Information.DirectoryName,
                            ShowCmd.SW_SHOWNORMAL);
                    }
                    break;
                case SettingsViewModel.CmdStrExploreFile:
                    if (File.Exists(this.Information.FullName))
                    {
                        CommonUtils.OpenFolderAndSelectItem(this.Information.FullName);
                    }
                    break;
                case SettingsViewModel.CmdStrShowFileProperties:
                    if (File.Exists(this.Information.FullName))
                    {
                        SHELLEXECUTEINFOW shellExecuteInformation = new SHELLEXECUTEINFOW();
                        shellExecuteInformation.cbSize = Marshal.SizeOf(shellExecuteInformation);
                        shellExecuteInformation.fMask = SEMaskFlags.SEE_MASK_INVOKEIDLIST;
                        shellExecuteInformation.hwnd = MainWindow.WndHandle;
                        shellExecuteInformation.lpVerb = "properties";
                        shellExecuteInformation.lpFile = this.Information.FullName;
                        shellExecuteInformation.lpDirectory = this.Information.DirectoryName;
                        shellExecuteInformation.nShow = ShowCmd.SW_SHOWNORMAL;
                        SHELL32.ShellExecuteExW(ref shellExecuteInformation);
                    }
                    break;
                case SettingsViewModel.CmdStrCopyCurHash:
                    if (this.GenerateTextInFormat(format: null, this.SelectedOutputType, all: false,
                        endLine: false, seeExport: false, casedName: false) is string hashValue)
                    {
                        CommonUtils.ClipboardSetText(hashValue);
                    }
                    break;
                case SettingsViewModel.CmdStrCopyAllHash:
                    if (this.GenerateTextInFormat(format: null, this.SelectedOutputType, all: true,
                        endLine: false, seeExport: false, casedName: false) is string allHashValues)
                    {
                        CommonUtils.ClipboardSetText(allHashValues);
                    }
                    break;
                case SettingsViewModel.CmdStrCopyCurHashByTemplate:
                    this.CopyThisModelCurHashAction(null);
                    break;
                case SettingsViewModel.CmdStrCopyAllHashByTemplate:
                    this.CopyThisModelAllHashesAction(null);
                    break;
                case SettingsViewModel.CmdStrCopyFileName:
                    CommonUtils.ClipboardSetText(this.Information.Name);
                    break;
                case SettingsViewModel.CmdStrCopyFilePath:
                    if (!this.Arguments.Deprecated)
                    {
                        CommonUtils.ClipboardSetText(this.Information.FullName);
                    }
                    else
                    {
                        NotificationSender.SnackbarWarning("文件不存在，未复制完整路径！");
                    }
                    break;
            }
        }
    }

    public ICommand TableColumnDoubleClickCmd
    {
        get
        {
            this.tableColumnDoubleClickCmd ??= new RelayCommand(this.TableColumnDoubleClickAction);
            return this.tableColumnDoubleClickCmd;
        }
    }

    public bool ReadAndPopulateHcmData()
    {
        try
        {
            using (FileStream fileStream = this.Information.OpenRead())
            {
                if (new HcmDataHelper(fileStream).ReadHcmData(out HcmData hcmData))
                {
                    this.HcmDataFromFile = hcmData;
                    return true;
                }
            }
        }
        catch (Exception)
        {
        }
        this.HcmDataFromFile = null;
        return false;
    }

    private void MakeSureAlgoModelArrayNotEmpty()
    {
        if (this.AlgoInOutModels == null || this.AlgoInOutModels.Count == 0)
        {
            this.AlgoInOutModels = new ObservableCollection<AlgoInOutModel>(
                AlgorithmsModel.GetSelectedAlgos());
        }
        this.CurrentInOutModel = this.AlgoInOutModels[0];
        foreach (AlgoInOutModel model in this.AlgoInOutModels)
        {
            model.SetHashResultChangedHandler(this.CurrentHashStringHandler);
        }
    }

    private void CurrentHashStringHandler(object sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(this.CurrentInOutModel) ||
            e.PropertyName == nameof(AlgoInOutModel.HashResult) ||
            e.PropertyName == nameof(this.SelectedOutputType)) &&
            this.CurrentInOutModel != null && this.CurrentInOutModel.HashResult != null)
        {
            if (this.SelectedOutputType != OutputType.Unknown)
            {
                this.CurrentHashString = BytesToStrByOutputTypeCvt.Convert(
                    this.CurrentInOutModel.HashResult, this.SelectedOutputType);
            }
            else
            {
                this.CurrentHashString = BytesToStrByOutputTypeCvt.Convert(
                    this.CurrentInOutModel.HashResult, Settings.Current.SelectedOutputType);
            }
        }
    }

    public void ResetHashViewModel()
    {
        this.IsExecutionTarget = false;
        this.HashGroupID = null;
        this.DurationofTask = double.NaN;
        this.Progress = 0;
        this.MaxProgress = 0;
        // 设置 this.State 后 ErrorDetails 也被自动设置
        this.State = HashState.NoState;
        this.desiredState = HashState.NoState;
        // 上一轮若以暂停收场，暂停信号仍处在阻断状态，此处必须解除：
        // 否则重新开始的这一轮会在第一个数据块处永久阻塞
        this.manualPauseController.Set();
        this.Result = HashResult.NoResult;
        try
        {
            if (!this.Arguments.Deprecated)
            {
                this.FileLength = this.Information.Length;
            }
            else
            {
                this.FileLength = -1;
            }
        }
        catch (Exception e) when (e is IOException || e is FileNotFoundException)
        {
            this.FileLength = -1;
        }
        if (this.AlgoInOutModels != null)
        {
            foreach (AlgoInOutModel model in this.AlgoInOutModels)
            {
                model.HashResult = null;
                model.Export = false;
                model.HashCmpResult = CmpRes.NoResult;
            }
        }
        this.cancellation = new CancellationTokenSource();
        this.cancellation.Token.Register(() =>
        {
            if (this.Result == HashResult.NoResult)
            {
                this.Result = HashResult.Canceled;
            }
        });
    }

    /// <summary>
    /// 由调度器在作业启动前调用，为重新计算准备必要的前置状态。<br/>
    /// 触发界面通知，故须在锁外调用。<br/>
    /// force 为 false 且作业是"计算缺值项"（已结束未成功）时，同样清空以便重算；<br/>
    /// force 为 true 表示强制重新计算：清空已使用的算法与输出方式，使作业按当前设置重新计算。
    /// </summary>
    internal void PrepareForRestart(bool force)
    {
        if (force || (this.State == HashState.Finished && this.Result != HashResult.Succeeded))
        {
            this.AlgoInOutModels = null;
            this.SelectedOutputType = OutputType.Unknown;
        }
    }

    public void ShutdownModelWait()
    {
        this.cancellation?.Cancel();
        this.manualPauseController.Set();
        Monitor.Enter(this.computeHashOperationLock);
        if (this.State == HashState.NoState || this.State == HashState.Waiting)
        {
            this.State = HashState.Finished;
        }
        Monitor.Exit(this.computeHashOperationLock);
    }

    /// <summary>
    /// 更新界面状态，由调度器在作业状态发生变更时调用。<br/>
    /// 异步分支读取的是 desiredState 的最新值而非入参，故即便本次投影晚于随后的
    /// 状态变更才执行，也不会把已经定稿的新状态覆盖回去。<br/>
    /// 已终结的作业不再接受排队态或运行态的投影：作废的提交其投影可能晚于作业终结
    /// 才执行，届时会把已终结状态覆盖成排队中。重新开始时 ResetHashViewModel 会把
    /// 期望状态重置为初始态，故不影响再次计算。
    /// </summary>
    internal void SetStateAsync(HashState state)
    {
        if (this.desiredState == HashState.Finished && state != HashState.NoState)
        {
            return;
        }
        this.desiredState = state;
        if (synchronization.CheckAccess())
        {
            this.State = state;
        }
        else
        {
            synchronization.BeginInvoke(new Action(() => { this.State = this.desiredState; }));
        }
    }

    /// <summary>
    /// 仅置位或解除暂停信号，不触碰任何界面绑定属性。<br/>
    /// 调度器必须在持有作业集合锁的情况下调用本方法，故本方法绝不能回界面线程，
    /// 否则会与正在等待该锁的界面线程互等而死锁。
    /// </summary>
    internal void SetPauseSignal(bool pause)
    {
        if (!pause)
        {
            this.manualPauseController.Set();
        }
        else
        {
            this.manualPauseController.Reset();
        }
    }

    /// <summary>
    /// 当前是否处于暂停状态。基于实际暂停信号而非界面投影 State 判断，<br/>
    /// 因此该值是同步且权威的，不受 State 异步更新窗口期的影响。
    /// </summary>
    internal bool IsPaused => !this.manualPauseController.WaitOne(0);

    /// <summary>
    /// 请求取消本次计算，同时解除暂停信号以唤醒可能正阻塞在暂停点的计算线程，
    /// 使其走到取消检查处退出。
    /// </summary>
    internal void RequestCancellation()
    {
        this.cancellation?.Cancel();
        this.manualPauseController.Set();
    }

    /// <summary>
    /// 把尚未开始计算的本模型终结为已取消。<br/>
    /// 已经派发过的作业走各自的取消与执行结束流程，故此处只对未开始的和排队中的生效。<br/>
    /// 必须显式设置 Result：从未启动过的作业其 cancellation 为 null，
    /// 不会触发 ResetHashViewModel 中注册的 Token 回调，Result 会一直停在无结果，
    /// 界面显示"无结果"而非"已取消"。
    /// </summary>
    internal void MarkCanceled()
    {
        if (this.State != HashState.NoState && this.State != HashState.Waiting)
        {
            return;
        }
        if (this.Result == HashResult.NoResult)
        {
            this.Result = HashResult.Canceled;
        }
        this.SetStateAsync(HashState.Finished);
    }

    public void SetHashCheckResultForModel(HashChecklist checklist)
    {
        if (checklist != null && this.AlgoInOutModels != null)
        {
            if (this.Result != HashResult.Succeeded)
            {
                foreach (AlgoInOutModel model in this.AlgoInOutModels)
                {
                    model.HashCmpResult = CmpRes.NoResult;
                }
            }
            else
            {
                if (checklist.TryGetFileOrEmptyStrHashChecker(this.RelativePath, out HashChecker checker))
                {
                    checker.SetModelCheckResult(this);
                }
                else
                {
                    foreach (AlgoInOutModel model in this.AlgoInOutModels)
                    {
                        model.HashCmpResult = CmpRes.Unrelated;
                    }
                }
                if (Settings.Current.AlgoToSwitchToAfterHashChecked != CmpRes.NoResult)
                {
                    foreach (AlgoInOutModel model in this.AlgoInOutModels)
                    {
                        if (model.HashCmpResult == Settings.Current.AlgoToSwitchToAfterHashChecked &&
                            (this.CurrentInOutModel == null || this.CurrentInOutModel.HashCmpResult != model.HashCmpResult))
                        {
                            this.CurrentInOutModel = model;
                            break;
                        }
                    }
                }
            }
        }
    }

    private void SetHashCheckResultForInOutModelAndSetCurModel()
    {
        if (this.AlgoInOutModels != null &&
            this.Arguments.HashChecklist?.TryGetFileOrEmptyStrHashChecker(this.RelativePath,
                out HashChecker checker) == true)
        {
            foreach (AlgoInOutModel item in this.AlgoInOutModels)
            {
                CmpRes hashCheckResult = checker.GetCheckResult(item.AlgoType, item.HashResult);
                item.HashCmpResult = hashCheckResult;
                if (Settings.Current.AlgoToSwitchToAfterHashChecked != CmpRes.NoResult &&
                    hashCheckResult == Settings.Current.AlgoToSwitchToAfterHashChecked &&
                    (this.CurrentInOutModel == null || this.CurrentInOutModel.HashCmpResult != hashCheckResult))
                {
                    this.CurrentInOutModel = item;
                }
            }
        }
    }

    public void ComputeManyHashValue()
    {
        Monitor.Enter(this.computeHashOperationLock);
        if (this.cancellation.IsCancellationRequested)
        {
            Monitor.Exit(this.computeHashOperationLock);
            return;
        }
        this.HasBeenRun = true;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        synchronization.Invoke(() =>
        {
            this.State = HashState.Running;
        });
        if (this.Arguments.Deprecated)
        {
            synchronization.Invoke(() =>
            {
                this.Result = HashResult.Failed;
                this.ErrorDetails = this.Arguments.Message;
            });
            goto FinishingTouchesBeforeExiting;
        }
        // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
        else if (!File.Exists(this.Information.FullName))
        {
            synchronization.Invoke(() =>
            {
                this.Result = HashResult.Failed;
                this.ErrorDetails = "此文件不存在或无法访问...";
            });
            goto FinishingTouchesBeforeExiting;
        }
        byte[] buffer = null;
        try
        {
            using (FileStream fs = this.Information.OpenRead())
            {
                synchronization.Invoke(() =>
                {
                    this.MakeSureAlgoModelArrayNotEmpty();
                    // 刷新大小，应对文件被添加后，计算前发生变化或被替换的情况
                    this.FileLength = fs.Length;
                    this.Progress = 0L;
                    this.MaxProgress = fs.Length;
                    if (this.SelectedOutputType == OutputType.Unknown)
                    {
                        this.SelectedOutputType = Settings.Current.SelectedOutputType;
                    }
                });
                if (fs.Length == 0 && Settings.Current.DoNotHashForEmptyFile)
                {
                    synchronization.Invoke(() =>
                    {
                        this.Result = HashResult.Failed;
                        this.ErrorDetails = "是空文件，终止计算并标记为失败...";
                    });
                    goto FinishingTouchesBeforeExiting;
                }
                foreach (AlgoInOutModel model in this.AlgoInOutModels)
                {
                    model.Algo.Initialize();
                }
                int actualReadCount = 0;
                CommonUtils.Suggest(ref buffer, this.FileLength);
                Action<int> updateProgress = size => { this.Progress += size; };
                bool terminateByCancellation = false;
                if (Settings.Current.ParallelBetweenAlgos)
                {
                    int minThreads = this.AlgoInOutModels.Count;
                    ThreadPool.GetMinThreads(out int minwt, out int mincpt);
                    if (minwt < minThreads)
                    {
                        _ = ThreadPool.SetMinThreads(minThreads, mincpt);
                    }
                    using (Barrier barrier = new Barrier(minThreads, i =>
                        {
                            stopwatch.Stop();
                            this.manualPauseController.WaitOne();
                            stopwatch.Start();
                            actualReadCount = fs.Read(buffer, 0, buffer.Length);
                            synchronization.BeginInvoke(updateProgress, actualReadCount);
                        }))
                    {
                        void DoTransformBlocks(AlgoInOutModel model)
                        {
                            while (true)
                            {
                                barrier.SignalAndWait();
                                if (this.cancellation.IsCancellationRequested)
                                {
                                    barrier.RemoveParticipant();
                                    terminateByCancellation = true;
                                    break;
                                }
                                if (actualReadCount <= 0)
                                {
                                    break;
                                }
                                model.Algo.TransformBlock(buffer, 0, actualReadCount, null, 0);
                            }
                        }
                        Parallel.ForEach(this.AlgoInOutModels, DoTransformBlocks);
                    }
                }
                else
                {
                    while (true)
                    {
                        stopwatch.Stop();
                        this.manualPauseController.WaitOne();
                        stopwatch.Start();
                        if (this.cancellation.IsCancellationRequested)
                        {
                            terminateByCancellation = true;
                            break;
                        }
                        if ((actualReadCount = fs.Read(buffer, 0, buffer.Length)) <= 0)
                        {
                            break;
                        }
                        foreach (AlgoInOutModel algoInOut in this.AlgoInOutModels)
                        {
                            algoInOut.Algo.TransformBlock(buffer, 0, actualReadCount, null, 0);
                        }
                        synchronization.BeginInvoke(updateProgress, actualReadCount);
                    }
                }
                if (!terminateByCancellation)
                {
                    Action<AlgoInOutModel> updateHashBytes = i =>
                    {
                        i.Export = true;
                        i.HashResult = i.Algo.Hash;
                    };
                    foreach (AlgoInOutModel item in this.AlgoInOutModels)
                    {
                        item.Algo.TransformFinalBlock(buffer, 0, 0);
                        synchronization.Invoke(updateHashBytes, item);
                    }
                    synchronization.Invoke(() =>
                    {
                        this.SetHashCheckResultForInOutModelAndSetCurModel();
                        this.Result = HashResult.Succeeded;
                    });
                }
            }
        }
        catch
        {
            synchronization.Invoke(() =>
            {
                this.Result = HashResult.Failed;
                this.ErrorDetails = "文件读取失败或进行计算时出错...";
            });
        }
        finally
        {
            CommonUtils.MakeSureBuffer(ref buffer, 0);
        }
    FinishingTouchesBeforeExiting:
        if (this.AlgoInOutModels != null)
        {
            foreach (AlgoInOutModel model in this.AlgoInOutModels)
            {
                model.Algo.Dispose();
            }
        }
        stopwatch.Stop();
        double duration = stopwatch.Elapsed.TotalSeconds;
        synchronization.Invoke(() =>
        {
            this.DurationofTask = duration;
            this.State = HashState.Finished;
        });
        Monitor.Exit(this.computeHashOperationLock);
    }

    public string GenerateTextInFormat(string format, OutputType output, bool all, bool endLine,
        bool seeExport, bool casedName)
    {
        if (this.Result == HashResult.Succeeded)
        {
            if (!all)
            {
                if (this.CurrentInOutModel != null)
                {
                    return this.CurrentInOutModel.GenerateTextInFormat(this, format, output, endLine,
                        seeExport, casedName);
                }
            }
            else
            {
                if (this.AlgoInOutModels != null && this.AlgoInOutModels.Any())
                {
                    StringBuilder stringBuilderForGenerateFormattedHash = new StringBuilder();
                    foreach (AlgoInOutModel inOutModel in this.AlgoInOutModels)
                    {
                        if (inOutModel.GenerateTextInFormat(
                            this, format, output, endLine: true, seeExport, casedName) is string text)
                        {
                            stringBuilderForGenerateFormattedHash.Append(text);
                        }
                    }
                    if (!endLine && stringBuilderForGenerateFormattedHash.Length > 0)
                    {
                        stringBuilderForGenerateFormattedHash.Remove(stringBuilderForGenerateFormattedHash.Length - 1, 1);
                    }
                    return stringBuilderForGenerateFormattedHash.ToString();
                }
            }
        }
        return default(string);
    }
}
