using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HashCalculator.Others;
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

    /// <summary>
    /// 本轮要计算的算法（运行计划的一部分）；为 null 表示清单内全部算法。
    /// 由 PrepareForRestartModel 依据计算意图设置，计算结束在 finally 中清空。
    /// </summary>
    private HashSet<AlgoType> _algoTypeFilter = null;
    private HashState _currentState = HashState.NoState;
    private HashResult _currentResult = HashResult.NoResult;
    private OutputType _selectedOutput = OutputType.Unknown;
    private RelayCommand shutdownModelSelfCmd;
    private RelayCommand restartModelSelfCmd;
    private RelayCommand pauseOrResumeModelSelfCmd;
    private RelayCommand copyThisModelCurHashCmd;
    private RelayCommand copyThisModelAllHashesCmd;
    private RelayCommand tableColumnDoubleClickCmd;

    private readonly ManualResetEvent _manualPauseController =
        new ManualResetEvent(true);
    private readonly object _hashComputationExclusiveLock = new object();

    /// <summary>
    /// 用户本次追加、尚未被启动消费的临时算法。临时算法只服务一轮：
    /// 启动时由 PrepareForRestartModel 清空，故不会影响下一轮计算。
    /// </summary>
    private readonly HashSet<AlgoType> _pendingTempAlgos = new HashSet<AlgoType>();
    private CancellationTokenSource cancellation;

    /// <summary>
    /// 调度器期望本模型处于的状态。<br/>
    /// 调度线程与界面线程都会改写 State，异步投影可能用过期值覆盖新状态，
    /// 故统一在此记录最新期望值，投影时读取本字段而非 State，保证最终显示最后一次的状态决定。
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
        this.InitializeAlgoInOutModels();
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
        set
        {
            if (ReferenceEquals(this._currentInOutModel, value))
            {
                return;
            }
            // 对 HashResult 的订阅跟随"当前显示算法"：退订旧的、订阅新的
            this._currentInOutModel?.PropertyChanged -= this.HashResultChangedHandler;
            this.SetPropNotify(ref this._currentInOutModel, value);
            this._currentInOutModel?.PropertyChanged += this.HashResultChangedHandler;
            // 切换当前显示的算法后，显示的哈希值要立刻跟着换成本算法的哈希值
            this.UpdateCurrentHashStringAction();
        }
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
        set
        {
            this.SetPropNotify(ref this._selectedOutput, value);
            // 输出方式变了就按新格式重算当前显示行
            this.UpdateCurrentHashStringAction();
        }
    }

    private void CopyThisModelCurHashAction(object param)
    {
        string format = Settings.Current.GenerateTextInFormat ?
            Settings.Current.FormatForGenerateText : null;
        if (this.GenerateTextInFormat(format, this.SelectedOutputType, all: false, endLine: false,
            seeExport: false, Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
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
        if (this.GenerateTextInFormat(format, this.SelectedOutputType, all: true, endLine: false,
            seeExport: false, Settings.Current.CaseOfCopiedAlgNameFollowsOutputType) is string text)
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
        JobScheduler.Current?.Start(this, ComputeIntent.FillMissing);
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

    /// <summary>
    /// 确立本任务的初始算法集：优先按哈希值信息文件/命令行指定的算法（PresetAlgos），
    /// 其次按校验清单（启用 PreferChecklistAlgs 时），都取不到则用当前勾选的算法兜底，
    /// 保证集合非空并把当前显示算法指向集合的第一个。
    /// </summary>
    private void InitializeAlgoInOutModels()
    {
        if (this.AlgoInOutModels?.Count > 0)
        {
            return;
        }
        if (this.Arguments.PresetAlgos != null)
        {
            this.AlgoInOutModels = AlgorithmsModel.GetKnownAlgos(this.Arguments.PresetAlgos);
        }
        else if (Settings.Current.PreferChecklistAlgs && this.Arguments.HashChecklist != null)
        {
            this.AlgoInOutModels = AlgorithmsModel.GetAlgsFromChecklist(this.Arguments.HashChecklist,
                this.RelativePath);
        }
        if ((this.AlgoInOutModels?.Count > 0) == false)
        {
            this.AlgoInOutModels = new ObservableCollection<AlgoInOutModel>(
                AlgorithmsModel.GetSelectedAlgos());
        }
        this.CurrentInOutModel = this.AlgoInOutModels[0];
    }

    /// <summary>
    /// 是否存在尚无结果的算法行（用于"计算缺值项"：只补算这些行）。<br/>
    /// 任务级的 Result 只反映整轮计算是否成功，无法表达"部分行有结果、部分行没有"，
    /// 故以行上的 HashResult 是否为空作为缺值判据。
    /// </summary>
    public bool HasMissingResults => this.AlgoInOutModels?.Any(
        m => m.HashResult == null) == true;

    /// <summary>
    /// 本任务能否按指定意图启动计算（准入属于任务自身的业务规则，故由本类提供）。<br/>
    /// - Recompute：已结束（重算）；<br/>
    /// - AppendTemporary：已结束（追加临时算法：旧行结果保留，取消/失败的任务旧行留空）；<br/>
    /// - FillMissing：未开始（首次启动），或已结束但未成功 / 仍有缺结果的算法行（计算缺值项）。<br/>
    /// 判据一律读 desiredState（权威最新值）而非 State：State 由异步投影写入，
    /// 调度线程及刚结束瞬间可能仍是旧值，用它会误拒本应放行的启动。
    /// </summary>
    internal bool CanStart(ComputeIntent intent)
    {
        return intent switch
        {
            // 重算与追加临时算法都要求已结束，区别只在 PrepareAlgoTypeFilter 定下的计算范围
            ComputeIntent.Recompute or ComputeIntent.AppendTemporary =>
                this.desiredState == HashState.Finished,
            // ComputeIntent.FillMissing
            _ => this.desiredState == HashState.NoState || (this.desiredState == HashState.Finished &&
                (this.Result != HashResult.Succeeded || this.HasMissingResults)
            ),
        };
    }

    /// <summary>
    /// 当前显示行的 HashResult 变化时刷新显示的哈希值。<br/>
    /// 订阅关系由 CurrentInOutModel 的 setter 维护，只挂在当前显示行上，无需各处手动挂载。
    /// </summary>
    private void HashResultChangedHandler(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AlgoInOutModel.HashResult))
        {
            this.UpdateCurrentHashStringAction();
        }
    }

    /// <summary>
    /// 按当前显示行的哈希结果与输出方式，重算 CurrentHashString。
    /// </summary>
    private void UpdateCurrentHashStringAction()
    {
        byte[] result = this.CurrentInOutModel?.HashResult;
        if (result is not null)
        {
            OutputType output = this.SelectedOutputType != OutputType.Unknown ?
            this.SelectedOutputType : Settings.Current.SelectedOutputType;
            this.CurrentHashString = BytesToStrByOutputTypeCvt.Convert(result, output);
            return;
        }
        // 当前行还没算出结果时清空显示，避免残留上一行的哈希值
        this.CurrentHashString = null;
    }

    /// <summary>
    /// 由调度器在作业启动前调用，重置任务级状态，并按计算意图准备算法清单，为重新派发计算做准备。<br/>
    /// 三种意图：<br/>
    /// - Recompute：按当前设置的勾选算法重建整份算法集（丢弃现有行）；<br/>
    /// - AppendTemporary：保留清单与已有结果，只补算临时追加的算法；<br/>
    /// - FillMissing：保留清单，只补算尚无结果的算法行。
    /// </summary>
    public void PrepareForRestartModel(ComputeIntent intent)
    {
        // 判断依据须在重置 State/Result 之前求值
        bool resetOutputType = intent ==
            ComputeIntent.Recompute ||
            (this.desiredState == HashState.Finished &&
            this.Result != HashResult.Succeeded);
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
        this._manualPauseController.Set();
        this.Result = HashResult.NoResult;
        if (resetOutputType)
        {
            this.SelectedOutputType = OutputType.Unknown;
        }
        // 依意图定下本轮算哪些算法，并应用到清单与算法筛选器
        this.PrepareAlgoTypeFilter(intent);
        if (intent == ComputeIntent.AppendTemporary)
        {
            // 临时算法只服务一轮，本轮启动后就可以清除登记了，避免影响下一轮
            this._pendingTempAlgos.Clear();
        }
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
    /// 依计算意图定下本轮算哪些算法，并把结果应用到算法清单与算法筛选器。<br/>
    /// 筛选器为 null 表示丢弃现有清单、按当前勾选的算法重建并全部计算；非 null（可为空集）
    /// 表示保留清单，只算集合内的算法行。
    /// - Recompute：null；<br/>
    /// - AppendTemporary：本次追加的临时算法；<br/>
    /// - FillMissing：清单中尚无结果的算法行。
    /// </summary>
    private void PrepareAlgoTypeFilter(ComputeIntent intent)
    {
        HashSet<AlgoType> algoTypes = intent switch
        {
            ComputeIntent.Recompute => null,
            ComputeIntent.AppendTemporary => new(this._pendingTempAlgos),
            // ComputeIntent.FillMissing
            _ => this.AlgoInOutModels.Where(i => i.HashResult == null)
                .Select(a => a.AlgoType).ToHashSet(),
        };
        if (algoTypes == null)
        {
            // 整体替换引用：比逐条增删更干脆，且天然触发绑定刷新
            this.AlgoInOutModels = new ObservableCollection<AlgoInOutModel>(
                AlgorithmsModel.GetSelectedAlgos());
        }
        else
        {
            this.EnsureRowsForAlgos(algoTypes);
        }
        this.EnsureCurrentInOutModelValid();
        this._algoTypeFilter = algoTypes;
    }

    /// <summary>
    /// 保证清单中存在这些算法的行；缺失的按模板补一行（不替换已有行，行实例可跨轮复用）。
    /// </summary>
    private void EnsureRowsForAlgos(HashSet<AlgoType> algoTypes)
    {
        foreach (AlgoType algoType in algoTypes)
        {
            if (this.AlgoInOutModels.Any(m => m.AlgoType == algoType))
            {
                continue;
            }
            if (algoType.ToAlgoInOutModel() is AlgoInOutModel model)
            {
                this.AlgoInOutModels.Add(model);
            }
        }
    }

    /// <summary>
    /// 保证当前显示算法是清单成员：清单被整体替换或增删后调用，避免指向已移除的行而悬空。
    /// </summary>
    private void EnsureCurrentInOutModelValid()
    {
        if (this.CurrentInOutModel is null ||
            this.AlgoInOutModels?.Contains(this.CurrentInOutModel) != true)
        {
            this.CurrentInOutModel = this.AlgoInOutModels?.Count > 0
                ? this.AlgoInOutModels[0] : null;
        }
    }

    public void ShutdownModelWait()
    {
        this.cancellation?.Cancel();
        this._manualPauseController.Set();
        Monitor.Enter(this._hashComputationExclusiveLock);
        if (this.State == HashState.NoState || this.State == HashState.Waiting)
        {
            this.State = HashState.Finished;
        }
        Monitor.Exit(this._hashComputationExclusiveLock);
    }

    /// <summary>
    /// 更新界面状态，由调度器在作业状态发生变更时调用。<br/>
    /// 异步分支读取的是 desiredState 的最新值而非入参，故即便本次投影晚于随后的
    /// 状态变更，也不会把已经定稿的新状态覆盖回去。<br/>
    /// 已终结的作业不再接受排队态或运行态的投影：作废的提交其投影可能晚于作业终结，
    /// 届时会把已终结状态覆盖成排队中。重新开始时 PrepareForRestartModel 会把
    /// 期望状态重置为初始态，故不影响再次计算。
    /// </summary>
    internal void SetStateAsync(HashState state)
    {
        if (state != HashState.NoState && this.desiredState == HashState.Finished)
        {
            return;
        }
        this.desiredState = state;
        if (Synchronization.UI.CheckAccess())
        {
            this.State = state;
        }
        else
        {
            Synchronization.UI.BeginInvoke(() => { this.State = this.desiredState; });
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
            this._manualPauseController.Set();
        }
        else
        {
            this._manualPauseController.Reset();
        }
    }

    /// <summary>
    /// 当前是否处于暂停状态。基于实际暂停信号而非界面投影 State 判断，<br/>
    /// 因此该值是同步且权威的，不受 State 异步更新窗口期的影响。
    /// </summary>
    internal bool IsPaused => !this._manualPauseController.WaitOne(0);

    /// <summary>
    /// 请求取消本次计算，同时解除暂停信号以唤醒可能正阻塞在暂停点的计算线程，
    /// 使其走到取消检查处退出。
    /// </summary>
    internal void RequestCancellation()
    {
        this.cancellation?.Cancel();
        this._manualPauseController.Set();
    }

    /// <summary>
    /// 把尚未开始计算的本模型终结为已取消。<br/>
    /// 已经派发过的作业走各自的取消与执行结束流程，故此处只对未开始的和排队中的。<br/>
    /// 必须显式设置 Status：从未启动过的作业其 cancellation 为 null，不会触发
    /// PrepareForRestartModel 中注册的 Token 回调，Status 会一直停在无结果，界面
    /// 显示"无结果"而非"已取消"。
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

    /// <summary>
    /// 用清单比对各算法结果，把比对结果（HashCmpResult）落到对应 AlgoInOutModel。<br/>
    /// 仅当 Result 为 Succeeded 时才真正比对，否则全部置 NoResult。
    /// 副作用：命中 AlgoToSwitchToAfterHashChecked 指定的结果时，会把 CurrentInOutModel 切到该算法。
    /// </summary>
    /// <param name="checklist">用于比对的校验清单。</param>
    public void ApplyHashCmpResult(HashChecklist checklist)
    {
        if (checklist == null || this.AlgoInOutModels == null)
        {
            return;
        }
        if (this.Result != HashResult.Succeeded)
        {
            foreach (AlgoInOutModel model in this.AlgoInOutModels)
            {
                model.HashCmpResult = CmpRes.NoResult;
            }
            return;
        }
        if (checklist.TryGetFileOrEmptyStrHashChecker(this.RelativePath, out HashChecker checker))
        {
            checker.SetComparisonResult(this);
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

    public bool AddAddTemporaryAlgorithm(AlgoType algoType)
    {
        // 只允许对已算完的任务追加临时算法
        // 与启动准入同一判据，取消/失败的任务旧行会留空
        if (!this.CanStart(ComputeIntent.AppendTemporary))
        {
            return false;
        }
        // 清单里已有该算法则不再追加
        if (this.AlgoInOutModels.Any(m => m.AlgoType == algoType))
        {
            return false;
        }
        // 先拿到待插入的新行：查不到模板直接失败，此时尚未产生任何副作用
        if (algoType.ToAlgoInOutModel() is not AlgoInOutModel model)
        {
            return false;
        }
        if (!this._pendingTempAlgos.Add(algoType))
        {
            return false;
        }
        // 立即插入行并切换到它，便于用户看到并查看其哈希值
        this.AlgoInOutModels.Add(model);
        this.CurrentInOutModel = model;
        return true;
    }

    public void ComputeManyHashValue()
    {
        byte[] buffer = null;
        AlgoInOutModel[] frozenAlgoModels = null;
        Stopwatch stopwatch = null;
        Monitor.Enter(this._hashComputationExclusiveLock);
        try
        {
            if (this.cancellation.IsCancellationRequested)
            {
                return;
            }
            this.HasBeenRun = true;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Synchronization.UI.Invoke(() => this.State = HashState.Running);
            if (this.Arguments.Deprecated)
            {
                Synchronization.UI.Invoke(
                    () => { this.Result = HashResult.Failed; this.ErrorDetails = this.Arguments.Message; }
                    );
                return;
            }
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.Information.FullName))
            {
                Synchronization.UI.Invoke(
                    () => { this.Result = HashResult.Failed; this.ErrorDetails = "此文件不存在或无法访问..."; }
                    );
                return;
            }
            using (FileStream fs = this.Information.OpenRead())
            {
                Synchronization.UI.Invoke(() =>
                {
                    // 应对文件被添加后，计算前发生变化或被替换的情况
                    this.FileLength = fs.Length;
                    this.Progress = 0L;
                    this.MaxProgress = fs.Length;
                    if (this.SelectedOutputType == OutputType.Unknown)
                    {
                        this.SelectedOutputType = Settings.Current.SelectedOutputType;
                    }
                    // 正常情况下集合已在构造或重算时建立，此处多为幂等返回
                    //this.InitializeAlgoInOutModels();
                });
                if (fs.Length == 0 && Settings.Current.DoNotHashForEmptyFile)
                {
                    Synchronization.UI.Invoke(() =>
                    {
                        this.Result = HashResult.Failed;
                        this.ErrorDetails = "是空文件，终止计算并标记为失败...";
                    });
                    return;
                }
                // 只读快照：本轮要算的行 = 清单中命中算法筛选器的行（筛选器为 null 即全部）
                frozenAlgoModels = this.AlgoInOutModels.Where(
                    model => this._algoTypeFilter?.Contains(model.AlgoType) != false
                    ).ToArray();
                foreach (AlgoInOutModel model in frozenAlgoModels)
                {
                    model.Algo.Initialize();
                }
                int actualReadCount = 0;
                CommonUtils.Suggest(ref buffer, this.FileLength);
                Action<int> updateProgress = size => this.Progress += size;
                bool terminateByCancellation = false;
                if (frozenAlgoModels.Length > 1 && Settings.Current.ParallelBetweenAlgos)
                {
                    int minThreads = frozenAlgoModels.Length;
                    ThreadPool.GetMinThreads(out int minwt, out int mincpt);
                    if (minwt < minThreads)
                    {
                        _ = ThreadPool.SetMinThreads(minThreads, mincpt);
                    }
                    using (Barrier barrier = new Barrier(minThreads, i =>
                        {
                            stopwatch.Stop();
                            this._manualPauseController.WaitOne();
                            stopwatch.Start();
                            actualReadCount = fs.Read(buffer, 0, buffer.Length);
                            Synchronization.UI.BeginInvoke(updateProgress, actualReadCount);
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
                        Parallel.ForEach(frozenAlgoModels, DoTransformBlocks);
                    }
                }
                else
                {
                    while (true)
                    {
                        stopwatch.Stop();
                        this._manualPauseController.WaitOne();
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
                        foreach (AlgoInOutModel algoInOut in frozenAlgoModels)
                        {
                            algoInOut.Algo.TransformBlock(buffer, 0, actualReadCount, null, 0);
                        }
                        Synchronization.UI.BeginInvoke(updateProgress, actualReadCount);
                    }
                }
                if (!terminateByCancellation)
                {
                    Action<AlgoInOutModel> updateHashBytes = i =>
                    {
                        i.Export = true;
                        i.HashResult = i.Algo.Hash;
                    };
                    foreach (AlgoInOutModel item in frozenAlgoModels)
                    {
                        item.Algo.TransformFinalBlock(buffer, 0, 0);
                        Synchronization.UI.Invoke(updateHashBytes, item);
                    }
                    Synchronization.UI.Invoke(() =>
                    {
                        // 必须先设置 Succeeded，否则 ApplyHashCmpResult 一律设置 NoResult
                        this.Result = HashResult.Succeeded;
                        this.ApplyHashCmpResult(this.Arguments.HashChecklist);
                    });
                }
            }
        }
        catch
        {
            Synchronization.UI.Invoke(
                () => { this.Result = HashResult.Failed; this.ErrorDetails = "文件读取失败或进行计算时出错..."; }
                );
        }
        finally
        {
            // 算法清单是"本轮"限定，一轮算完（含取消/异常）即失效，避免残留影响下一轮
            // 否则某轮追加临时算法留下的子集会让下次"全部重算/计算缺值项"漏算其它算法
            this._algoTypeFilter = null;
            CommonUtils.MakeSureBuffer(ref buffer, 0);
            if (frozenAlgoModels != null)
            {
                foreach (AlgoInOutModel model in frozenAlgoModels)
                {
                    // 只释放非托管状态，不调用 HashAlgorithm.Dispose —— 后者会让实例永久不可用，
                    // 而算法实例需跨轮复用（下轮 Initialize 会重新创建状态）
                    model.IAlgo.Release();
                }
            }
            if (stopwatch is not null)
            {
                stopwatch.Stop();
                double duration = stopwatch.Elapsed.TotalSeconds;
                Synchronization.UI.Invoke(() => { this.DurationofTask = duration; this.State = HashState.Finished; });
            }
            Monitor.Exit(this._hashComputationExclusiveLock);
        }
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
