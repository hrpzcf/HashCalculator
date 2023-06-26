using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class HashViewModel : NotifiableModel
    {
        #region properties for binding
        private string _hashString = string.Empty;
        private string _modelDetails = "暂无详情...";
        private bool _exportHash = false;
        private double durationofTask = 0.0;
        private long _fileSize = 0L;
        private long _progress = 0L;
        private long _progressTotal = 0L;
        private AlgoType _hashAlgoType = AlgoType.Unknown;
        private CmpRes _cmpResult = CmpRes.NoResult;
        private HashResult _currentResult = HashResult.NoResult;
        private OutputType selectedOutputType = OutputType.Unknown;
        private volatile HashState _currentState = HashState.NoState;
        private RelayCommand shutdownModelSelfCmd;
        private RelayCommand restartModelSelfCmd;
        private RelayCommand pauseOrContinueModelSelfCmd;
        private RelayCommand copyOneModelHashValueCmd;
        #endregion

        private readonly byte[] expectedHash;
        private CancellationTokenSource cancellation;
        private readonly bool isDeprecated;
        private static readonly Dispatcher synchronization =
            Application.Current.Dispatcher;
        private readonly ManualResetEvent manualPauseController =
            new ManualResetEvent(true);
        private readonly object hashComputeOperationLock = new object();

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
            this.Serial = serial;
            this.ModelArg = arg;
            this.FileInfo = new FileInfo(arg.filepath);
            this.FileName = this.FileInfo.Name;
            this.expectedHash = arg.expected;
            this.isDeprecated = arg.deprecated;
        }

        public int Serial { get; private set; }

        public string FileName { get; private set; }

        public FileInfo FileInfo { get; private set; }

        public ModelArg ModelArg { get; }

        // Xaml 绑定会更改此值，不使用 private set
        public bool Export
        {
            get
            {
                return this._exportHash;
            }
            set
            {
                this.SetPropNotify(ref this._exportHash, value);
            }
        }

        /// <summary>
        /// 如果计算未完成，即使“导出”（Export）被用户勾上也不会被导出。
        /// 因为计算未完成时 IsSucceeded 为 false，导出结果时同时验证 IsSucceeded 和 Export。
        /// </summary>
        public bool IsSucceeded
        {
            get
            {
                return this.Result == HashResult.Succeeded;
            }
        }

        public byte[] Hash { get; private set; }

        public string HashString
        {
            get
            {
                return this._hashString;
            }
            private set
            {
                this.SetPropNotify(ref this._hashString, value);
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

        public AlgoType HashAlgoType
        {
            get
            {
                return this._hashAlgoType;
            }
            private set
            {
                this.SetPropNotify(ref this._hashAlgoType, value);
            }
        }

        // 校验哈希值时外部会更改此值，不使用 private set
        public CmpRes CmpResult
        {
            get
            {
                return this._cmpResult;
            }
            set
            {
                this.SetPropNotify(ref this._cmpResult, value);
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
                this._currentState = value;
                switch (value)
                {
                    case HashState.NoState:
                        this.HashString = string.Empty;
                        break;
                    case HashState.Waiting:
                        this.HashString = "任务排队中...";
                        break;
                }
                this.NotifyPropertyChanged();
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
                switch (value)
                {
                    case HashResult.Canceled:
                        this.HashString = "任务已取消...";
                        break;
                }
                this.SetPropNotify(ref this._currentResult, value);
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

        public long ProgressTotal
        {
            get
            {
                return this._progressTotal;
            }
            private set
            {
                this.SetPropNotify(ref this._progressTotal, value);
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
                return this.durationofTask;
            }
            private set
            {
                this.SetPropNotify(ref this.durationofTask, value);
            }
        }

        // Xaml 绑定会更改此值，不使用 private set
        public OutputType SelectedOutputType
        {
            get
            {
                return this.selectedOutputType;
            }
            set
            {
                this.SetPropNotify(ref this.selectedOutputType, value);
                if (this.Hash != null)
                {
                    this.HashString = (string)HashBytesOutputTypeCvt.Convert(this.Hash, value);
                }
            }
        }

        private void CopyOneModelHashValueAction(object param)
        {
            if (this.Result != HashResult.Succeeded)
            {
                return;
            }
            if (this.SelectedOutputType != OutputType.Unknown)
            {
                Clipboard.SetText(this.HashString);
            }
            else
            {
                Clipboard.SetText((string)HashBytesOutputTypeCvt.Convert(
                    this.Hash, Settings.Current.SelectedOutputType));
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

        public bool HasBeenRun { get; private set; }

        public ControlItem[] AvailableOutputTypes { get; } =
        {
            new ControlItem("没有指定", OutputType.Unknown),
            new ControlItem("Base64", OutputType.BASE64),
            new ControlItem("Hex大写", OutputType.BinaryUpper),
            new ControlItem("Hex小写", OutputType.BinaryLower),
        };

        private void ModelCancelled()
        {
            if (!this.IsSucceeded)
            {
                this.Result = HashResult.Canceled;
            }
        }

        public void ResetHashViewModel()
        {
            this.cancellation = new CancellationTokenSource();
            this.cancellation.Token.Register(this.ModelCancelled);
            this.Hash = null;
            this.DurationofTask = 0.0;
            this.Progress = 0;
            this.ProgressTotal = 0;
            this.State = HashState.NoState;
            this.Result = HashResult.NoResult;
            this.CmpResult = CmpRes.NoResult;
            this.HashAlgoType = AlgoType.Unknown;
        }

        public bool StartupModel(bool force)
        {
            bool startupModelResult = false;
            if (Monitor.TryEnter(this.hashComputeOperationLock))
            {
                bool conditionMatched;
                if (force)
                {
                    this.SelectedOutputType = OutputType.Unknown;
                    conditionMatched = this.State == HashState.Finished;
                }
                else
                {
                    // 右键选择任务控制时有可能尝试启动 State 为 Waiting 的 HashViewModel
                    // 但 Waiting 的 HashViewModel 不该被再次启动，因为 Waiting 代表已排队待计算
                    conditionMatched = this.State == HashState.NoState ||
                        (this.State == HashState.Finished && this.Result != HashResult.Succeeded);
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
            AlgoType algoType = Settings.Current.SelectedAlgo;
            synchronization.Invoke(() =>
            {
                this.HashAlgoType = algoType;
                this.State = HashState.Running;
            });
            if (this.isDeprecated)
            {
                synchronization.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.HashString = "搜索不到此文件...";
                });
                goto FinishingTouchesBeforeExiting;
            }
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
            {
                synchronization.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.HashString = "此文件不存在或无法访问...";
                });
                goto FinishingTouchesBeforeExiting;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.FileInfo.FullName))
                {
                    synchronization.Invoke(() =>
                    {
                        this.FileSize = fs.Length;
                        this.Progress = 0L;
                        this.ProgressTotal = this.FileSize;
                    });
                    HashAlgorithm algoObject;
                    switch (algoType)
                    {
                        case AlgoType.SHA1:
                            algoObject = new SHA1Cng();
                            break;
                        case AlgoType.SHA224:
                            algoObject = new BouncyCastleSha224Digest();
                            break;
                        default:
                        case AlgoType.SHA256:
                            algoObject = new SHA256Cng();
                            break;
                        case AlgoType.SHA384:
                            algoObject = new SHA384Cng();
                            break;
                        case AlgoType.SHA512:
                            algoObject = new SHA512Cng();
                            break;
                        case AlgoType.SHA3_224:
                            algoObject = new BouncyCastleSha3Digest(224);
                            break;
                        case AlgoType.SHA3_256:
                            algoObject = new BouncyCastleSha3Digest(256);
                            break;
                        case AlgoType.SHA3_384:
                            algoObject = new BouncyCastleSha3Digest(384);
                            break;
                        case AlgoType.SHA3_512:
                            algoObject = new BouncyCastleSha3Digest(512);
                            break;
                        case AlgoType.MD5:
                            algoObject = new MD5Cng();
                            break;
                        case AlgoType.BLAKE2s:
                            algoObject = new BouncyCastleIDigest<Blake2sDigest>();
                            break;
                        case AlgoType.BLAKE2b:
                            algoObject = new BouncyCastleIDigest<Blake2bDigest>();
                            break;
                        case AlgoType.BLAKE3:
                            algoObject = new BouncyCastleIDigest<Blake3Digest>();
                            break;
                        case AlgoType.Whirlpool:
                            algoObject = new BouncyCastleIDigest<WhirlpoolDigest>();
                            break;
                    }
                    using (algoObject)
                    {
                        int readedSize = 0;
                        byte[] buffer = new byte[BufferSize.Suggest(this.FileSize)];
                        while (true)
                        {
                            if (this.cancellation.IsCancellationRequested)
                            {
                                goto FinishingTouchesBeforeExiting;
                            }
                            readedSize = fs.Read(buffer, 0, buffer.Length);
                            if (readedSize <= 0)
                            {
                                break;
                            }
                            algoObject.TransformBlock(buffer, 0, readedSize, null, 0);
                            synchronization.Invoke(
                                () => { this.Progress += readedSize; }, DispatcherPriority.Background);
                            this.manualPauseController.WaitOne();
                        }
                        algoObject.TransformFinalBlock(buffer, 0, 0);
                        this.Hash = algoObject.Hash;
                        CmpRes comparisonResult = CmpRes.NoResult;
                        if (this.expectedHash != null)
                        {
                            if (!this.expectedHash.Any())
                            {
                                comparisonResult = CmpRes.Uncertain;
                            }
                            else
                            {
                                if (this.expectedHash.SequenceEqual(this.Hash))
                                {
                                    comparisonResult = CmpRes.Matched;
                                }
                                else
                                {
                                    comparisonResult = CmpRes.Mismatch;
                                }
                            }
                        }
                        synchronization.Invoke(() =>
                        {
                            this.Export = true;
                            if (this.SelectedOutputType != OutputType.Unknown)
                            {
                                // 用于触发刷新 this.HashString
                                this.SelectedOutputType = this.SelectedOutputType;
                            }
                            else
                            {
                                this.SelectedOutputType = Settings.Current.SelectedOutputType;
                            }
                            this.CmpResult = comparisonResult;
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
                    this.HashString = "此文件读取失败或计算出错...";
                });
            }
        FinishingTouchesBeforeExiting:
            stopwatch.Stop();
            double duration = stopwatch.Elapsed.TotalSeconds;
            synchronization.Invoke(() =>
            {
                this.DurationofTask = duration;
                this.ModelDetails = $"文件名称：{this.FileName}\n文件大小：{CommonUtils.FileSizeCvt(this.FileSize)}\n"
                    + $"任务运行时长：{duration:f2}秒";
                this.State = HashState.Finished;
            });
            this.ModelReleasedEvent?.InvokeAsync(this);
            this.ModelShutdownEvent?.Invoke(this);
            Monitor.Exit(this.hashComputeOperationLock);
        }
    }
}
