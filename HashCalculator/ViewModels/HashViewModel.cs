using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HashCalculator
{
    internal class BouncyCastSha224 : HashAlgorithm
    {
        private readonly Sha224Digest sha224digest;

        public BouncyCastSha224()
        {
            this.sha224digest = new Sha224Digest();
        }

        public override void Initialize()
        {
            this.sha224digest?.Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.sha224digest.BlockUpdate(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            int size = this.sha224digest.GetDigestSize();
            byte[] sha224ComputeResult = new byte[size];
            this.sha224digest.DoFinal(sha224ComputeResult, 0);
            return sha224ComputeResult;
        }
    }

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
        private AlgoType _hashName = AlgoType.Unknown;
        private CmpRes _cmpResult = CmpRes.NoResult;
        private HashState _currentState = HashState.Waiting;
        private HashResult _currentResult = HashResult.NoResult;
        private OutputType selectedOutputType = OutputType.Unknown;
        private RelayCommand copyOneModelHashValueCmd;
        #endregion

        private readonly byte[] expectedHash;
        private CancellationTokenSource cancellation;
        private readonly bool isDeprecated;
        private const int blockSize = 2097152;
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private readonly ManualResetEvent manualPauseController = new ManualResetEvent(true);
        private readonly object hashComputeOperationLock = new object();

        /// <summary>
        /// 调用 StartupModel 方法且满足相关条件则触发此事件
        /// </summary>
        public event Action<HashViewModel> ModelCapturedEvent;

        /// <summary>
        /// 调用 ShutdownModel 方法或哈希值计算完成则触发此事件
        /// </summary>
        public event Action<HashViewModel> ModelReleasedEvent;

        public HashViewModel(int serial, ModelArg arg)
        {
            this.Serial = serial;
            this.FileInfo = new FileInfo(arg.filepath);
            this.FileName = this.FileInfo.Name;
            this.expectedHash = arg.expected;
            this.isDeprecated = arg.deprecated;
        }

        public int Serial { get; private set; }

        public string FileName { get; private set; }

        public FileInfo FileInfo { get; private set; }

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

        public AlgoType HashName
        {
            get
            {
                return this._hashName;
            }
            private set
            {
                this.SetPropNotify(ref this._hashName, value);
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
                switch (value)
                {
                    case HashState.Waiting:
                        this.HashString = "任务排队中...";
                        break;
                }
                this.SetPropNotify(ref this._currentState, value);
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

        public bool HasBeenRun { get; private set; }

        public ControlItem[] AvailableOutputTypes { get; } =
        {
            new ControlItem("没有指定", OutputType.Unknown),
            new ControlItem("Base64", OutputType.BASE64),
            new ControlItem("Hex大写", OutputType.BinaryUpper),
            new ControlItem("Hex小写", OutputType.BinaryLower),
        };

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

        private void ModelCancelled()
        {
            if (!this.IsSucceeded)
            {
                this.Result = HashResult.Canceled;
            }
        }

        private void ResetHashViewModel()
        {
            this.cancellation = new CancellationTokenSource();
            this.cancellation.Token.Register(this.ModelCancelled);
            this.DurationofTask = 0.0;
            this.CmpResult = CmpRes.NoResult;
            this.Result = HashResult.NoResult;
            this.State = HashState.Waiting;
            this.Hash = null;
            this.Progress = this.ProgressTotal = 0;
            this.HashName = AlgoType.Unknown;
        }

        public bool StartupModel(bool force)
        {
            bool result = false;
            if (Monitor.TryEnter(this.hashComputeOperationLock))
            {
                if (force)
                {
                    this.SelectedOutputType = OutputType.Unknown;
                }
                if (force || this.State == HashState.Waiting
                    || (this.State == HashState.Finished
                    && this.Result != HashResult.Succeeded))
                {
                    this.ResetHashViewModel();
                    this.ModelCapturedEvent?.InvokeAsync(this);
                    result = true;
                }
                Monitor.Exit(this.hashComputeOperationLock);
            }
            return result;
        }

        public void ShutdownModel()
        {
            if (Monitor.TryEnter(this.hashComputeOperationLock))
            {
                this.cancellation?.Cancel();
                this.manualPauseController.Set();
                if (this.State == HashState.Waiting)
                {
                    this.State = HashState.Finished;
                    this.ModelReleasedEvent?.InvokeAsync(this);
                }
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
            AppDispatcher.Invoke(() =>
            {
                this.HashName = algoType;
                this.State = HashState.Running;
            });
            HashAlgorithm algoObject;
            switch (algoType)
            {
                case AlgoType.SHA1:
                    algoObject = new SHA1Cng();
                    break;
                case AlgoType.SHA384:
                    algoObject = new SHA384Cng();
                    break;
                case AlgoType.SHA512:
                    algoObject = new SHA512Cng();
                    break;
                case AlgoType.MD5:
                    algoObject = new MD5Cng();
                    break;
                case AlgoType.SHA224:
                    algoObject = new BouncyCastSha224();
                    break;
                case AlgoType.SHA256:
                default:
                    algoObject = new SHA256Cng();
                    break;
            }
            if (this.isDeprecated)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.HashString = "在依据所在文件夹中找不到此文件...";
                });
                goto TaskRunningEnds;
            }
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.HashString = "要计算哈希值的文件不存在或无法访问...";
                });
                goto TaskRunningEnds;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.FileInfo.FullName))
                {
                    AppDispatcher.Invoke(() =>
                    {
                        this.Progress = 0L;
                        this.ProgressTotal = fs.Length;
                    });
                    using (algoObject)
                    {
                        int readedSize = 0;
                        byte[] buffer = new byte[blockSize];
                        while (true)
                        {
                            if (this.cancellation.IsCancellationRequested)
                            {
                                goto TaskRunningEnds;
                            }
                            readedSize = fs.Read(buffer, 0, buffer.Length);
                            if (readedSize <= 0)
                            {
                                break;
                            }
                            AppDispatcher.Invoke(() => { this.Progress += readedSize; });
                            algoObject.TransformBlock(buffer, 0, readedSize, null, 0);
                            this.manualPauseController.WaitOne();
                        }
                        algoObject.TransformFinalBlock(buffer, 0, 0);
                        AppDispatcher.Invoke(() =>
                        {
                            this.Hash = algoObject.Hash;
                            if (this.SelectedOutputType != OutputType.Unknown)
                            {
                                // 用于触发刷新 this.HashString
                                this.SelectedOutputType = this.SelectedOutputType;
                            }
                            else
                            {
                                this.SelectedOutputType = Settings.Current.SelectedOutputType;
                            }
                            this.Export = true;
                            this.FileSize = fs.Length;
                        });
                        if (this.expectedHash != null)
                        {
                            CmpRes result;
                            if (!this.expectedHash.Any())
                            {
                                result = CmpRes.Uncertain;
                            }
                            else
                            {
                                if (this.expectedHash.SequenceEqual(this.Hash))
                                {
                                    result = CmpRes.Matched;
                                }
                                else
                                {
                                    result = CmpRes.Mismatch;
                                }
                            }
                            AppDispatcher.Invoke(() => { this.CmpResult = result; });
                        }
                    }
                }
                AppDispatcher.Invoke(() => { this.Result = HashResult.Succeeded; });
            }
            catch
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
                    this.HashString = "读取文件失败或哈希值计算出错...";
                });
            }
        TaskRunningEnds:
            stopwatch.Stop();
            double duration = stopwatch.Elapsed.TotalSeconds;
            AppDispatcher.Invoke(() =>
            {
                this.DurationofTask = duration;
                this.ModelDetails = $"文件名称：{this.FileName}\n文件大小：{CommonUtils.FileSizeCvt(this.FileSize)}\n"
                    + $"任务运行时长：{duration:f2}秒";
                this.State = HashState.Finished;
            });
            this.ModelReleasedEvent?.InvokeAsync(this);
            Monitor.Exit(this.hashComputeOperationLock);
        }
    }
}
