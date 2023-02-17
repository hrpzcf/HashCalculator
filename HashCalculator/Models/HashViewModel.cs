using Org.BouncyCastle.Crypto.Digests;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    // 封装这个类计算文件哈希值，虽然实现了与其他哈希值算法代码的统一
    // 但计算 216MB 文件的 SHA224 比手动投喂 Sha224Digest 慢 0.2 秒左右
    // 手动投喂：使用 UsingBouncyCastleSha224 方法
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

    internal class HashViewModel : INotifyPropertyChanged
    {
        #region properties for binding

        private string _hashValue = "正在排队...";
        private bool _exportHash = false;
        private CmpRes _cmpResult = CmpRes.NoResult;
        private HashState _currentState = HashState.Waiting;
        private HashResult _currentResult = HashResult.NoResult;
        private long _progress = 0L;
        private long _progressTotal = 0L;
        private string _modelDetails = "暂无详情...";
        private AlgoType _hashName = AlgoType.Unknown;
        private string durationofTask = string.Empty;
        #endregion

        private readonly string expectedHash;
        private CancellationToken token;
        private CancellationTokenSource tokenSource;
        private readonly bool isDeprecated;
        private const int blockSize = 2097152;
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private readonly object manipulationLock = new object();
        private readonly ManualResetEvent pauseManualResetEvent
            = new ManualResetEvent(true);
        private readonly object cmpResultLock = new object();

        public event Action<HashViewModel> ModelCanbeStartedEvent;
        public event Action<int> ComputeFinishedEvent;
        public event Action<int> WaitingModelCanceledEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        public HashViewModel(int serial, ModelArg arg)
        {
            this.Path = new FileInfo(arg.filepath);
            this.Serial = serial;
            this.Name = this.Path.Name;
            this.expectedHash = arg.expected?.ToLower();
            this.isDeprecated = arg.deprecated;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int Serial { get; set; }

        public string Name { get; set; }

        public FileInfo Path { get; set; }

        public bool Export
        {
            get { return this._exportHash; }
            set { this._exportHash = value; this.OnPropertyChanged(); }
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

        public string Hash
        {
            get { return this._hashValue; }
            set { this._hashValue = value; this.OnPropertyChanged(); }
        }

        public AlgoType HashName
        {
            get { return this._hashName; }
            set { this._hashName = value; this.OnPropertyChanged(); }
        }

        public CmpRes CmpResult
        {
            get
            {
                lock (cmpResultLock)
                    return this._cmpResult;
            }
            set
            {
                lock (cmpResultLock)
                {
                    this._cmpResult = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public HashState State
        {
            get { return this._currentState; }
            set
            {
                switch (value)
                {
                    case HashState.Waiting:
                        this.Hash = "正在排队...";
                        break;
                }
                this._currentState = value;
                this.OnPropertyChanged();
            }
        }

        public HashResult Result
        {
            get { return this._currentResult; }
            set
            {
                switch (value)
                {
                    case HashResult.Canceled:
                        this.Hash = "任务已被取消...";
                        break;
                }
                this._currentResult = value;
                this.OnPropertyChanged();
            }
        }

        public long Progress
        {
            get { return this._progress; }
            set { this._progress = value; this.OnPropertyChanged(); }
        }

        public long ProgressTotal
        {
            get { return this._progressTotal; }
            set { this._progressTotal = value; this.OnPropertyChanged(); }
        }

        public string ModelDetails
        {
            get { return this._modelDetails; }
            set { this._modelDetails = value; this.OnPropertyChanged(); }
        }

        public string DurationofTask
        {
            get { return this.durationofTask; }
            set { this.durationofTask = value; this.OnPropertyChanged(); }
        }

        public void HashViewModelCancelled()
        {
            if (this.IsSucceeded)
                return;
            AppDispatcher.Invoke(() =>
            {
                this.HashName = AlgoType.Unknown;
                this.Result = HashResult.Canceled;
            });
        }

        public void ComputeManyHashValue()
        {
            long fileSize = 0L;
            HashAlgorithm algoObject;
            AlgoType algoType;
            if (this.token.IsCancellationRequested)
                return;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            lock (Locks.AlgoSelectionLock)
            {
                algoType = Settings.Current.SelectedAlgo;
                AppDispatcher.Invoke(() =>
                {
                    this.HashName = algoType;
                    this.State = HashState.Running;
                });
            }
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
                    this.UsingBouncyCastleSha224(stopwatch);
                    return;
                //hashAlgo = new BouncyCastSha224();
                //break;
                case AlgoType.SHA256:
                default:
                    algoObject = new SHA256Cng();
                    break;
            }
            if (this.isDeprecated)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.HasFailed;
                    this.Hash = "在依据所在文件夹中找不到此文件";
                });
                goto TaskRunningEnds;
            }
            else if (!this.Path.Exists)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.HasFailed;
                    this.Hash = "要计算哈希值的文件不存在或无法访问";
                });
                goto TaskRunningEnds;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    AppDispatcher.Invoke(() =>
                    {
                        this.Progress = 0L;
                        this.ProgressTotal = fileSize = fs.Length;
                    });
                    using (algoObject)
                    {
                        int readedSize = 0;
                        byte[] buffer = new byte[blockSize];
                        while (true)
                        {
                            if (this.token.IsCancellationRequested)
                                goto TaskRunningEnds;
                            readedSize = fs.Read(buffer, 0, buffer.Length);
                            if (readedSize <= 0) break;
                            AppDispatcher.Invoke(() => { this.Progress += readedSize; });
                            algoObject.TransformBlock(buffer, 0, readedSize, null, 0);
                            this.pauseManualResetEvent.WaitOne();
                        }
                        algoObject.TransformFinalBlock(buffer, 0, 0);
                        string hashStr = BitConverter.ToString(algoObject.Hash).Replace("-", "");
                        if (Settings.Current.UseLowercaseHash)
                            hashStr = hashStr.ToLower();
                        AppDispatcher.Invoke(
                            () => { this.Export = true; this.Hash = hashStr; });
                        if (this.expectedHash != null)
                        {
                            CmpRes result;
                            if (this.expectedHash == string.Empty)
                                result = CmpRes.Uncertain;
                            else if (hashStr.ToLower() == this.expectedHash)
                                result = CmpRes.Matched;
                            else
                                result = CmpRes.Mismatch;
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
                    this.Result = HashResult.HasFailed;
                    this.Hash = "读取文件失败或哈希值计算出错";
                });
            }
        TaskRunningEnds:
            stopwatch.Stop();
            AppDispatcher.Invoke(() => { this.State = HashState.Finished; });
            string durationof = $"{stopwatch.Elapsed.TotalSeconds:f2}";
            AppDispatcher.Invoke(() =>
            {
                this.DurationofTask = durationof;
                this.ModelDetails = $"文件名称：{this.Name}\n文件大小：{UnitCvt.FileSizeCvt(fileSize)}\n"
                + $"任务运行时长：{durationof}秒";
            });
            this.ComputeFinishedEvent?.Invoke(1);
        }

        private void UsingBouncyCastleSha224(Stopwatch stopwatch)
        {
            long fileSize = 0L;
            if (this.isDeprecated)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.HasFailed;
                    this.Hash = "在依据所在文件夹中找不到此文件";
                });
                goto TaskRunningEnds;
            }
            else if (!this.Path.Exists)
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.HasFailed;
                    this.Hash = "要计算哈希值的文件不存在或无法访问";
                });
                goto TaskRunningEnds;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    AppDispatcher.Invoke(() =>
                    {
                        this.Progress = 0L;
                        this.ProgressTotal = fileSize = fs.Length;
                    });
                    int readedSize = 0;
                    Sha224Digest algorithmHash = new Sha224Digest();
                    byte[] buffer = new byte[blockSize];
                    while (true)
                    {
                        if (this.token.IsCancellationRequested)
                            goto TaskRunningEnds;
                        readedSize = fs.Read(buffer, 0, blockSize);
                        if (readedSize <= 0) break;
                        AppDispatcher.Invoke(() => { this.Progress += readedSize; });
                        algorithmHash.BlockUpdate(buffer, 0, readedSize);
                        this.pauseManualResetEvent.WaitOne();
                    }
                    int outLength = algorithmHash.DoFinal(buffer, 0);
                    string hashStr = BitConverter.ToString(buffer, 0, outLength).Replace("-", "");
                    if (Settings.Current.UseLowercaseHash)
                        hashStr = hashStr.ToLower();
                    AppDispatcher.Invoke(() => { this.Export = true; this.Hash = hashStr; });
                    if (this.expectedHash != null)
                    {
                        CmpRes result;
                        if (this.expectedHash == string.Empty)
                            result = CmpRes.Uncertain;
                        else if (hashStr.ToLower() == this.expectedHash)
                            result = CmpRes.Matched;
                        else
                            result = CmpRes.Mismatch;
                        AppDispatcher.Invoke(() => { this.CmpResult = result; });
                    }
                }
                AppDispatcher.Invoke(() => { this.Result = HashResult.Succeeded; });
            }
            catch
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.HasFailed;
                    this.Hash = "读取文件失败或哈希值计算出错";
                });
            }
        TaskRunningEnds:
            stopwatch.Stop();
            AppDispatcher.Invoke(() => { this.State = HashState.Finished; });
            string durationof = $"{stopwatch.Elapsed.TotalSeconds:f2}";
            AppDispatcher.Invoke(() =>
            {
                this.DurationofTask = durationof;
                this.ModelDetails = $"文件名称：{this.Name}\n文件大小：{UnitCvt.FileSizeCvt(fileSize)}\n"
                + $"任务运行时长：{durationof}秒";
            });
            this.ComputeFinishedEvent?.Invoke(1);
        }

        private void ResetHashViewModel()
        {
            this.pauseManualResetEvent.Set();
            this.DurationofTask = string.Empty;
            this.CmpResult = CmpRes.NoResult;
            this.Result = HashResult.NoResult;
            this.State = HashState.Waiting;
            this.Progress = this.ProgressTotal = 0;
            this.HashName = AlgoType.Unknown;
        }

        public bool StartupModel(bool force)
        {
            lock (manipulationLock)
            {
                if (force ||
                    this.State == HashState.Waiting ||
                    (this.State == HashState.Finished &&
                    this.Result != HashResult.Succeeded))
                {
                    this.ResetHashViewModel();
                    this.PrepareToken();
                    this.ModelCanbeStartedEvent(this);
                    return true;
                }
                return false;
            }
        }

        private bool PauseModel()
        {
            if (this.State == HashState.Running)
            {
                this.pauseManualResetEvent.Reset();
                this.State = HashState.Paused;
                return true;
            }
            return false;
        }

        private bool ContinueModel()
        {
            if (this.State == HashState.Paused)
            {
                this.pauseManualResetEvent.Set();
                this.State = HashState.Running;
                return true;
            }
            return false;
        }

        public bool PauseOrContinueModel(PauseMode mode)
        {
            lock (manipulationLock)
            {
                if (mode == PauseMode.Pause)
                    return this.PauseModel();
                else if (mode == PauseMode.Continue)
                    return this.ContinueModel();
                else if (mode == PauseMode.Invert)
                {
                    if (this.State == HashState.Running)
                        return this.PauseModel();
                    else if (this.State == HashState.Paused)
                        return this.ContinueModel();
                }
                return false;
            }
        }

        public void ShutdownModel()
        {
            lock (manipulationLock)
            {
                if (this.State == HashState.Finished)
                    return;
                this.pauseManualResetEvent.Set();
                if (this.tokenSource != null &&
                    !this.tokenSource.IsCancellationRequested)
                    this.tokenSource.Cancel();
                if (this.State == HashState.Waiting)
                {
                    this.State = HashState.Finished;
                    this.WaitingModelCanceledEvent?.Invoke(1);
                }
                else { this.State = HashState.Finished; }
            }
        }

        private void PrepareToken()
        {
            if (this.tokenSource == null || this.tokenSource.IsCancellationRequested)
                this.tokenSource = new CancellationTokenSource();
            this.token = this.tokenSource.Token;
            this.token.Register(this.HashViewModelCancelled);
        }
    }
}
