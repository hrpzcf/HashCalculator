using System;
using System.Buffers;
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
using Handy = HandyControl;

namespace HashCalculator
{
    internal class HashViewModel : NotifiableModel
    {
        private string _fileName = string.Empty;
        private string _currentHashString = null;
        private string _errorDetails = "任务未开始...";
        private string _modelDetails = "暂无详情...";
        private long _fileLength = 0L;
        private long _progress = 0L;
        private long _maxProgress = 0L;
        private double _durationofTask = 0.0;
        private bool _isExecutionTarget = false;
        private HcmData _hcmDataFromFile = null;
        private AlgoInOutModel _currentInOutModel = null;
        private AlgoInOutModel[] _algoInOutModels = null;
        private ComparableColor _tableRowColor = null;
        private ComparableColor _hashGroupId = null;
        private ComparableColor _embeddedHashGroupId = null;
        private ComparableColor _folderGroupId = null;
        private HashState _currentState = HashState.NoState;
        private HashResult _currentResult = HashResult.NoResult;
        private OutputType _selectedOutput = OutputType.Unknown;
        private RelayCommand shutdownModelSelfCmd;
        private RelayCommand restartModelSelfCmd;
        private RelayCommand pauseOrContinueModelSelfCmd;
        private RelayCommand copyThisModelCurHashCmd;
        private RelayCommand copyThisModelAllHashesCmd;
        private RelayCommand showHashDetailsWindowCmd;
        private RelayCommand tableColumnDoubleClickCmd;

        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private readonly ManualResetEvent manualPauseController =
            new ManualResetEvent(true);
        private readonly object computeHashOperationLock = new object();
        private CancellationTokenSource cancellation;

        /// <summary>
        /// 调用 StartupModel 方法且满足相关条件则触发此事件，
        /// 表示可以外部调用此事件发出的 HashViewModel 的 ComputeManyHashValue 方法。<br/>
        /// 此事件是异步事件。
        /// </summary>
        public event Action<HashViewModel> ModelCapturedEvent;

        /// <summary>
        /// 调用 ShutdownModel 方法时如果 State 是 Waitting，则退出时会触发此事件。
        /// 或者 HashViewModel 的 State 由 Running 变为 Finished 后触发也会触发此事件。<br/>
        /// 此事件是异步事件。
        /// </summary>
        public event Action<HashViewModel> ModelReleasedEvent;

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
                this.AlgoInOutModels = AlgosPanelModel.GetKnownAlgos(arg.PresetAlgos);
            }
            else if (Settings.Current.PreferChecklistAlgs && arg.HashChecklist != null)
            {
                this.AlgoInOutModels = AlgosPanelModel.GetAlgsFromChecklist(arg.HashChecklist,
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

        public ComparableColor TableRowColor
        {
            get => this._tableRowColor;
            set => this.SetPropNotify(ref this._tableRowColor, value);
        }

        /// <summary>
        /// 相同哈希值分组标识
        /// </summary>
        public ComparableColor GroupId
        {
            get => this._hashGroupId;
            set => this.SetPropNotify(ref this._hashGroupId, value);
        }

        /// <summary>
        /// 相同的内嵌哈希值分组标识
        /// </summary>
        public ComparableColor EhGroupId
        {
            get => this._embeddedHashGroupId;
            set => this.SetPropNotify(ref this._embeddedHashGroupId, value);
        }

        /// <summary>
        /// 相同文件夹分组标识
        /// </summary>
        public ComparableColor FdGroupId
        {
            get => this._folderGroupId;
            set => this.SetPropNotify(ref this._folderGroupId, value);
        }

        public AlgoInOutModel[] AlgoInOutModels
        {
            get => this._algoInOutModels;
            set => this.SetPropNotify(ref this._algoInOutModels, value);
        }

        public HashState State
        {
            get
            {
                return this._currentState;
            }
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
            get
            {
                return this._currentResult;
            }
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

        public string ModelDetails
        {
            get => this._modelDetails;
            set => this.SetPropNotify(ref this._modelDetails, value);
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
                Handy.Controls.Growl.Success($"已按模板复制当前哈希值：\n{text}",
                    MessageToken.MainWndMsgToken);
            }
        }

        public ICommand CopyThisModelCurHashCmd
        {
            get
            {
                if (this.copyThisModelCurHashCmd is null)
                {
                    this.copyThisModelCurHashCmd = new RelayCommand(this.CopyThisModelCurHashAction);
                }
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
                Handy.Controls.Growl.Success($"已按模板复制所有哈希值：\n{text}",
                    MessageToken.MainWndMsgToken);
            }
        }

        public ICommand CopyThisModelAllHashesCmd
        {
            get
            {
                if (this.copyThisModelAllHashesCmd is null)
                {
                    this.copyThisModelAllHashesCmd = new RelayCommand(this.CopyThisModelAllHashesAction);
                }
                return this.copyThisModelAllHashesCmd;
            }
        }

        private void ShutdownModelSelfAction(object param)
        {
            this.ShutdownModel();
        }

        public ICommand ShutdownModelSelfCmd
        {
            get
            {
                if (this.shutdownModelSelfCmd is null)
                {
                    this.shutdownModelSelfCmd = new RelayCommand(this.ShutdownModelSelfAction);
                }
                return this.shutdownModelSelfCmd;
            }
        }

        private void RestartModelSelfAction(object param)
        {
            this.StartupModel(false);
        }

        public ICommand RestartModelSelfCmd
        {
            get
            {
                if (this.restartModelSelfCmd is null)
                {
                    this.restartModelSelfCmd = new RelayCommand(this.RestartModelSelfAction);
                }
                return this.restartModelSelfCmd;
            }
        }

        private void PauseOrContinueModelSelfAction(object param)
        {
            this.PauseOrContinueModel(PauseMode.Invert);
        }

        public ICommand PauseOrContinueModelSelfCmd
        {
            get
            {
                if (this.pauseOrContinueModelSelfCmd is null)
                {
                    this.pauseOrContinueModelSelfCmd = new RelayCommand(this.PauseOrContinueModelSelfAction);
                }
                return this.pauseOrContinueModelSelfCmd;
            }
        }

        private void ShowHashDetailsWindowAction(object param)
        {
            new HashDetailsWnd(this) { Owner = MainWindow.Current }.ShowDialog();
        }

        public ICommand ShowHashDetailsWindowCmd
        {
            get
            {
                if (this.showHashDetailsWindowCmd == null)
                {
                    this.showHashDetailsWindowCmd = new RelayCommand(this.ShowHashDetailsWindowAction);
                }
                return this.showHashDetailsWindowCmd;
            }
        }

        private void TableColumnDoubleClickAction(object param)
        {
            if (param is string commandString)
            {
                switch (commandString)
                {
                    case SettingsViewModel.CmdStrShowDetails:
                        if (this.Result == HashResult.Succeeded)
                        {
                            this.ShowHashDetailsWindowAction(null);
                        }
                        else
                        {
                            Handy.Controls.Growl.Warning("没有完成哈希值计算！",
                                MessageToken.MainWndMsgToken);
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
                            var shellExecuteInformation = new SHELLEXECUTEINFOW();
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
                            Handy.Controls.Growl.Error("文件不存在，未复制完整路径！",
                                MessageToken.MainWndMsgToken);
                        }
                        break;
                }
            }
        }

        public ICommand TableColumnDoubleClickCmd
        {
            get
            {
                if (this.tableColumnDoubleClickCmd == null)
                {
                    this.tableColumnDoubleClickCmd = new RelayCommand(this.TableColumnDoubleClickAction);
                }
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
            if (this.AlgoInOutModels == null || this.AlgoInOutModels.Length == 0)
            {
                this.AlgoInOutModels = AlgosPanelModel.GetSelectedAlgos().ToArray();
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
            this.DurationofTask = 0.0;
            this.GroupId = null;
            this.Progress = 0;
            this.MaxProgress = 0;
            // 设置 this.State 后 ErrorDetails 也被自动设置
            this.State = HashState.NoState;
            this.Result = HashResult.NoResult;
            this.IsExecutionTarget = false;
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
                foreach (var model in this.AlgoInOutModels)
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

        public bool StartupModel(bool force)
        {
            return this.StartupModel(force, delay: 0);
        }

        public bool StartupModel(bool force, int delay)
        {
            bool startupModelResult = false;
            if (Monitor.TryEnter(this.computeHashOperationLock))
            {
                bool conditionMatched = false;
                if (force)
                {
                    this.AlgoInOutModels = null;
                    this.SelectedOutputType = OutputType.Unknown;
                    conditionMatched = this.State == HashState.Finished;
                }
                else
                {
                    // 程序内右键选择任务控制时有可能尝试启动 State 为 Waiting 的本类实例
                    // 但 Waiting 的 HashViewModel 不该被再次启动，因为 Waiting 代表已排队待计算
                    if (this.State == HashState.NoState)
                    {
                        // 此处不重置 AlgoInOutModels 属性是因为首次启动可能已经预置了此属性
                        // 例如从系统右键选择特定的哈希值启动计算，此类实例就预置了 AlgoInOutModels 属性
                        // 而 State == HashState.NoState 就代表是任务的首次启动
                        conditionMatched = true;
                    }
                    else if (this.State == HashState.Finished && this.Result != HashResult.Succeeded)
                    {
                        conditionMatched = true;
                        this.AlgoInOutModels = null;
                    }
                }
                if (conditionMatched)
                {
                    this.ResetHashViewModel();
                    this.ModelCapturedEvent?.InvokeAsync(this, delay);
                    startupModelResult = true;
                }
                Monitor.Exit(this.computeHashOperationLock);
            }
            return startupModelResult;
        }

        public void ShutdownModelWait()
        {
            this.cancellation?.Cancel();
            this.manualPauseController.Set();
            Monitor.Enter(this.computeHashOperationLock);
            if (this.State == HashState.NoState || this.State == HashState.Waiting)
            {
                this.State = HashState.Finished;
                // TODO: 是否需要 InvokeAsync?
                this.ModelReleasedEvent?.InvokeAsync(this);
            }
            Monitor.Exit(this.computeHashOperationLock);
        }

        public void ShutdownModel()
        {
            if (Monitor.TryEnter(this.computeHashOperationLock))
            {
                this.cancellation?.Cancel();
                switch (this.State)
                {
                    case HashState.NoState:
                        this.State = HashState.Finished;
                        this.Result = HashResult.Canceled;
                        break;
                    case HashState.Waiting:
                        this.State = HashState.Finished;
                        this.ModelReleasedEvent?.InvokeAsync(this);
                        break;
                }
                Monitor.Exit(this.computeHashOperationLock);
            }
            else
            {
                this.cancellation?.Cancel();
                this.manualPauseController.Set();
            }
        }

        private bool PauseModel()
        {
            if (this.State == HashState.Running)
            {
                this.manualPauseController.Reset();
                this.State = HashState.Paused;
                return true;
            }
            return false;
        }

        private bool ContinueModel()
        {
            if (this.State == HashState.Paused)
            {
                this.manualPauseController.Set();
                this.State = HashState.Running;
                return true;
            }
            else if (this.State == HashState.NoState)
            {
                return this.StartupModel(false);
            }
            return false;
        }

        public bool PauseOrContinueModel(PauseMode mode)
        {
            if (mode == PauseMode.Pause)
            {
                return this.PauseModel();
            }
            else if (mode == PauseMode.Continue)
            {
                return this.ContinueModel();
            }
            else if (mode == PauseMode.Invert)
            {
                if (this.State == HashState.Running)
                {
                    return this.PauseModel();
                }
                else if (this.State == HashState.Paused)
                {
                    return this.ContinueModel();
                }
            }
            return false;
        }

        private bool MarkAsWaiting()
        {
            // StartupModel 时已经重置了 State 和 Result，为什么这里还要判断？
            // 因为 StartupModel 里 ModelCapturedEvent 是异步调用的，有可能发生：
            // StartupModel 后 ShutdownModel 使状态变化才执行到 MarkAsWaiting
            if (this.State == HashState.NoState && this.Result != HashResult.Canceled)
            {
                this.State = HashState.Waiting;
                return true;
            }
            return false;
        }

        public bool MarkAsWaiting(bool queueContainsThis)
        {
            if (queueContainsThis)
            {
                synchronization.Invoke(() => { this.State = HashState.Waiting; });
                return false;
            }
            // 增加一个 private bool MarkAsWaiting 方法的原因是：
            // ModelCapturedEvent 是异步调用，所以调用链中的本函数是在子线程中执行，
            // State = HashState.Waiting 要在 synchronization 中执行以使界面及时变化，
            // 判断语句也放在 synchronization 执行的原因是：
            // 不会发生执行判断语句后、等待 synchronization.Invoke 前，主线程 ShutdownModel 使 State 和 Result 状态改变，
            // 因为 synchronization.Invoke 也是在主线程执行的，不会和主线程 ShutdownModel 同时发生
            return synchronization.Invoke(this.MarkAsWaiting);
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
                    if (checklist.TryGetFileOrEmptyHashChecker(this.RelativePath, out HashChecker checker))
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
                this.Arguments.HashChecklist?.TryGetFileOrEmptyHashChecker(this.RelativePath,
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
                        int modelsCount = this.AlgoInOutModels.Length;
                        using (Barrier barrier = new Barrier(modelsCount, i =>
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
                            ThreadPool.GetMinThreads(out int minwt, out int mincpt);
                            if (minwt < modelsCount)
                            {
                                ThreadPool.SetMinThreads(modelsCount, mincpt);
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
                                goto ReturnRentedBufferMemory;
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
                ReturnRentedBufferMemory:
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
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
                this.ModelDetails = $"文件名称：{this.FileName}\n文件大小：{CommonUtils.FileSizeCvt(this.FileLength)}\n"
                    + $"任务任务耗时：{duration:f2}秒";
                this.State = HashState.Finished;
            });
            this.ModelReleasedEvent?.InvokeAsync(this);
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
}
