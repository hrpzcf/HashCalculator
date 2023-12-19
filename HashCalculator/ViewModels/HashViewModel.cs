using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class HashViewModel : NotifiableModel
    {
        private string _fileName = string.Empty;
        private string _currentHashString = null;
        private string _taskDetails = string.Empty;
        private string _modelDetails = "暂无详情...";
        private long _fileSize = 0L;
        private long _progress = 0L;
        private long _maxProgress = 0L;
        private double _durationofTask = 0.0;
        private bool _isExecutionTarget = false;
        private AlgoInOutModel _currentInOutModel = null;
        private AlgoInOutModel[] _algoInOutModels = null;
        private ComparableColor _groupId = null;
        private ComparableColor _folderGroupId = null;
        private HashResult _currentResult = HashResult.NoResult;
        private HashState _currentState = HashState.NoState;
        private OutputType _selectedOutput = OutputType.Unknown;
        private RelayCommand shutdownModelSelfCmd;
        private RelayCommand restartModelSelfCmd;
        private RelayCommand pauseOrContinueModelSelfCmd;
        private RelayCommand copyOneModelHashValueCmd;
        private RelayCommand showHashDetailsWindowCmd;

        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private readonly ManualResetEvent manualPauseController =
            new ManualResetEvent(true);
        private readonly object hashComputeOperationLock = new object();
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

        /// <summary>
        /// 调用 ShutdownModel 方法时如果 State 是非 Running 则触发此事件后退出，
        /// 如果 State 是 Running 则由 Running 变为 Finished 后触发。<br/>
        /// 此事件是同步事件，可以保证在触发期间 HashViewModel 不会成功执行 StartupModel 和 ComputeManyHashValue。
        /// </summary>
        public event Action<HashViewModel> ModelShutdownEvent;

        public HashViewModel(int serial, ModelArg arg)
        {
            this.ModelArg = arg;
            this.SerialNumber = serial;
            this.InvalidFileName = arg.InvalidFileName;
            if (arg.InvalidFileName)
            {
                this.FileInfo = new FileInfo("无效的文件名");
            }
            else
            {
                this.FileInfo = new FileInfo(arg.FilePath);
            }
            this.FileName = this.FileInfo.Name;
            if (arg.PresetAlgos != null)
            {
                this.AlgoInOutModels = AlgosPanelModel.GetKnownAlgos(arg.PresetAlgos);
            }
            else if (Settings.Current.PreferChecklistAlgs && arg.HashChecklist != null)
            {
                this.AlgoInOutModels =
                    AlgosPanelModel.GetAlgsFromChecklist(arg.HashChecklist, this.FileName);
            }
            this.PropertyChanged += this.CurrentHashStringHandler;
        }

        public int SerialNumber { get; }

        public FileInfo FileInfo { get; }

        public bool InvalidFileName { get; }

        public ModelArg ModelArg { get; }

        public bool HasBeenRun { get; private set; }

        public bool Matched { get; set; } = true;

        public CmpableFileIndex FileIndex { get; set; }

        public string FileName
        {
            get
            {
                return this._fileName;
            }
            set
            {
                this.SetPropNotify(ref this._fileName, value);
            }
        }

        public long FileSize
        {
            get
            {
                return this._fileSize;
            }
            private set
            {
                this.SetPropNotify(ref this._fileSize, value);
            }
        }

        public string CurrentHashString
        {
            get
            {
                return this._currentHashString;
            }
            set
            {
                this.SetPropNotify(ref this._currentHashString, value);
            }
        }

        public AlgoInOutModel CurrentInOutModel
        {
            get
            {
                return this._currentInOutModel;
            }
            set
            {
                this.SetPropNotify(ref this._currentInOutModel, value);
            }
        }

        public ComparableColor GroupId
        {
            get
            {
                return this._groupId;
            }
            set
            {
                this.SetPropNotify(ref this._groupId, value);
            }
        }

        public ComparableColor FdGroupId
        {
            get
            {
                return this._folderGroupId;
            }
            set
            {
                this.SetPropNotify(ref this._folderGroupId, value);
            }
        }

        public AlgoInOutModel[] AlgoInOutModels
        {
            get
            {
                return this._algoInOutModels;
            }
            private set
            {
                this.SetPropNotify(ref this._algoInOutModels, value);
            }
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
                    this.TaskMessage = string.Empty;
                }
                else if (value == HashState.Waiting)
                {
                    this.TaskMessage = "任务排队中...";
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
                    this.TaskMessage = "任务已取消...";
                }
            }
        }

        public long Progress
        {
            get
            {
                return this._progress;
            }
            private set
            {
                this.SetPropNotify(ref this._progress, value);
            }
        }

        public long MaxProgress
        {
            get
            {
                return this._maxProgress;
            }
            private set
            {
                this.SetPropNotify(ref this._maxProgress, value);
            }
        }

        public string TaskMessage
        {
            get
            {
                return this._taskDetails;
            }
            set
            {
                this.SetPropNotify(ref this._taskDetails, value);
            }
        }

        public string ModelDetails
        {
            get
            {
                return this._modelDetails;
            }
            private set
            {
                this.SetPropNotify(ref this._modelDetails, value);
            }
        }

        public double DurationofTask
        {
            get
            {
                return this._durationofTask;
            }
            private set
            {
                this.SetPropNotify(ref this._durationofTask, value);
            }
        }

        public bool IsExecutionTarget
        {
            get
            {
                return this._isExecutionTarget;
            }
            set
            {
                this.SetPropNotify(ref this._isExecutionTarget, value);
            }
        }

        // Xaml 绑定会更改此值，不使用 private set
        public OutputType SelectedOutputType
        {
            get
            {
                return this._selectedOutput;
            }
            set
            {
                this.SetPropNotify(ref this._selectedOutput, value);
            }
        }

        private void CopyOneModelHashValueAction(object param)
        {
            if (this.Result == HashResult.Succeeded && !string.IsNullOrEmpty(this.CurrentHashString))
            {
                CommonUtils.ClipboardSetText(this.CurrentHashString);
            }
        }

        public ICommand CopyOneModelHashValueCmd
        {
            get
            {
                if (this.copyOneModelHashValueCmd is null)
                {
                    this.copyOneModelHashValueCmd =
                        new RelayCommand(this.CopyOneModelHashValueAction);
                }
                return this.copyOneModelHashValueCmd;
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
                    this.shutdownModelSelfCmd =
                        new RelayCommand(this.ShutdownModelSelfAction);
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
                    this.restartModelSelfCmd =
                        new RelayCommand(this.RestartModelSelfAction);
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
                    this.pauseOrContinueModelSelfCmd =
                        new RelayCommand(this.PauseOrContinueModelSelfAction);
                }
                return this.pauseOrContinueModelSelfCmd;
            }
        }

        private void ShowHashDetailsWindowAction(object param)
        {
            new HashDetailsWnd(this) { Owner = MainWindow.This }.ShowDialog();
        }

        public ICommand ShowHashDetailsWindowCmd
        {
            get
            {
                if (this.showHashDetailsWindowCmd == null)
                {
                    this.showHashDetailsWindowCmd =
                        new RelayCommand(this.ShowHashDetailsWindowAction);
                }
                return this.showHashDetailsWindowCmd;
            }
        }

        public GenericItemModel[] AvailableOutputTypes { get; } =
        {
            new GenericItemModel("Base64", OutputType.BASE64),
            new GenericItemModel("Hex大写", OutputType.BinaryUpper),
            new GenericItemModel("Hex小写", OutputType.BinaryLower),
        };

        private void MakeSureAlgoModelArrayNotEmpty()
        {
            if (!this.AlgoInOutModels?.Any() ?? true)
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
            this.TaskMessage = string.Empty;
            this.FileSize = 0;
            this.DurationofTask = 0.0;
            this.Progress = 0;
            this.MaxProgress = 0;
            this.State = HashState.NoState;
            this.Result = HashResult.NoResult;
            this.GroupId = null;
            this.IsExecutionTarget = false;
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
            bool startupModelResult = false;
            if (Monitor.TryEnter(this.hashComputeOperationLock))
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
                    this.ModelCapturedEvent?.InvokeAsync(this);
                    startupModelResult = true;
                }
                Monitor.Exit(this.hashComputeOperationLock);
            }
            return startupModelResult;
        }

        public void ShutdownModel()
        {
            if (Monitor.TryEnter(this.hashComputeOperationLock))
            {
                this.cancellation?.Cancel();
                if (this.State == HashState.NoState || this.State == HashState.Waiting)
                {
                    this.State = HashState.Finished;
                    this.ModelReleasedEvent?.InvokeAsync(this);
                }
                this.ModelShutdownEvent?.Invoke(this);
                Monitor.Exit(this.hashComputeOperationLock);
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
            // StartupModel 后 ShutdownModel 使状态变化才执行 MarkAsWaiting
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
                synchronization.Invoke(() =>
                {
                    this.State = HashState.Waiting;
                });
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

        public void ComputeManyHashValue()
        {
            Monitor.Enter(this.hashComputeOperationLock);
            if (this.cancellation.IsCancellationRequested)
            {
                Monitor.Exit(this.hashComputeOperationLock);
                return;
            }
            this.HasBeenRun = true;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            synchronization.Invoke(() =>
            {
                this.State = HashState.Running;
            });
            if (this.ModelArg.Deprecated)
            {
                synchronization.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.TaskMessage = "未搜索到文件，请检查搜索策略后重新添加...";
                });
                goto FinishingTouchesBeforeExiting;
            }
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
            {
                synchronization.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.TaskMessage = "该文件不存在或无法访问...";
                });
                goto FinishingTouchesBeforeExiting;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.FileInfo.FullName))
                {
                    synchronization.Invoke(() =>
                    {
                        this.MakeSureAlgoModelArrayNotEmpty();
                        this.FileSize = fs.Length;
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
                            this.TaskMessage = "是空文件，终止计算并标记为失败...";
                        });
                        goto FinishingTouchesBeforeExiting;
                    }
                    foreach (AlgoInOutModel model in this.AlgoInOutModels)
                    {
                        model.Algo.Initialize();
                    }
                    int readedSize = 0;
                    int bufferMinSize = BufferSize.Suggest(this.FileSize);
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferMinSize);
                    Action<int> updateProgress = size => { this.Progress += size; };
                    bool terminateByCancellation = false;
                    if (Settings.Current.ParallelBetweenAlgos)
                    {
                        int modelsCount = this.AlgoInOutModels.Length;
                        using (Barrier barrier = new Barrier(modelsCount, i =>
                            {
                                this.manualPauseController.WaitOne();
                                readedSize = fs.Read(buffer, 0, bufferMinSize);
                                synchronization.BeginInvoke(updateProgress, readedSize);
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
                                    if (readedSize <= 0)
                                    {
                                        break;
                                    }
                                    model.Algo.TransformBlock(buffer, 0, readedSize, null, 0);
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
                            this.manualPauseController.WaitOne();
                            if (this.cancellation.IsCancellationRequested)
                            {
                                goto ReturnRentedBufferMemory;
                            }
                            if ((readedSize = fs.Read(buffer, 0, bufferMinSize)) <= 0)
                            {
                                break;
                            }
                            foreach (AlgoInOutModel algoInOut in this.AlgoInOutModels)
                            {
                                algoInOut.Algo.TransformBlock(buffer, 0, readedSize, null, 0);
                            }
                            synchronization.BeginInvoke(updateProgress, readedSize);
                        }
                    }
                    if (!terminateByCancellation)
                    {
                        Action<AlgoInOutModel> updateHashBytes = i =>
                        {
                            i.Export = true;
                            i.HashResult = i.Algo.Hash;
                        };
                        Action<AlgoInOutModel, CmpRes> updateModel = (i, c) =>
                        {
                            i.HashCmpResult = c;
                        };
                        AlgHashMap algoHashMap =
                            this.ModelArg.HashChecklist?.GetAlgHashMapOfFile(this.FileName);
                        foreach (AlgoInOutModel item in this.AlgoInOutModels)
                        {
                            item.Algo.TransformFinalBlock(buffer, 0, 0);
                            synchronization.Invoke(updateHashBytes, item);
                            if (algoHashMap != null)
                            {
                                CmpRes bytesComparisonResult = algoHashMap.CompareHash(
                                    item.AlgoName, item.HashResult);
                                synchronization.Invoke(updateModel, item, bytesComparisonResult);
                            }
                        }
                        synchronization.Invoke(() =>
                        {
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
                    this.TaskMessage = "文件读取失败或进行计算时出错...";
                });
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
                this.ModelDetails = $"文件名称：{this.FileName}\n文件大小：{CommonUtils.FileSizeCvt(this.FileSize)}\n"
                    + $"任务任务耗时：{duration:f2}秒";
                this.State = HashState.Finished;
            });
            this.ModelReleasedEvent?.InvokeAsync(this);
            this.ModelShutdownEvent?.Invoke(this);
            Monitor.Exit(this.hashComputeOperationLock);
        }
    }
}
