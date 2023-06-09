using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Input;
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

    internal class HashViewModel : NotifiableModel
    {
        #region properties for binding

        private string _hashValue = "正在排队...";
        private long _fileSize = 0L;
        private bool _exportHash = false;
        private CmpRes _cmpResult = CmpRes.NoResult;
        private HashState _currentState = HashState.Waiting;
        private HashResult _currentResult = HashResult.NoResult;
        private long _progress = 0L;
        private long _progressTotal = 0L;
        private string _modelDetails = "暂无详情...";
        private AlgoType _hashName = AlgoType.Unknown;
        private string durationofTask = string.Empty;
        private RelayCommand copyModelHashValueCmd;
        private RelayCommand copyFileFullPathCmd;
        private RelayCommand openModelFilePathCmd;
        private RelayCommand openFolderSelectItemCmd;
        private RelayCommand openFilePropertiesCmd;
        #endregion

        private readonly string expectedHash;
        private CancellationToken token;
        private CancellationTokenSource tokenSource;
        private readonly bool isDeprecated;
        private const int blockSize = 2097152;
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private readonly ManualResetEvent pauseManualResetEvent
            = new ManualResetEvent(true);
        private readonly object cmpResultLock = new object();
        private readonly object manipulationLock = new object();
        private readonly object exportOptionLock = new object();

        public event Action<int> ComputeFinishedEvent;
        public event Action<int> WaitingModelCanceledEvent;
        public event Action<HashViewModel> ModelCanbeStartedEvent;

        public HashViewModel(int serial, ModelArg arg)
        {
            this.FileInfo = new FileInfo(arg.filepath);
            this.Serial = serial;
            this.Name = this.FileInfo.Name;
            this.expectedHash = arg.expected;
            this.isDeprecated = arg.deprecated;
        }

        public int Serial { get; set; }

        public string Name { get; set; }

        public FileInfo FileInfo { get; set; }

        public bool Export
        {
            get
            {
                lock (this.exportOptionLock)
                {
                    return this._exportHash;
                }
            }
            set
            {
                lock (this.exportOptionLock)
                {
                    this.SetPropNotify(ref this._exportHash, value);
                }
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

        public string Hash
        {
            get
            {
                return this._hashValue;
            }
            set
            {
                this.SetPropNotify(ref this._hashValue, value);
            }
        }

        public long FileSize
        {
            get
            {
                return this._fileSize;
            }
            set
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
            set
            {
                this.SetPropNotify(ref this._hashName, value);
            }
        }

        public CmpRes CmpResult
        {
            get
            {
                lock (this.cmpResultLock)
                {
                    return this._cmpResult;
                }
            }
            set
            {
                lock (this.cmpResultLock)
                {
                    this.SetPropNotify(ref this._cmpResult, value);
                }
            }
        }

        public HashState State
        {
            get
            {
                return this._currentState;
            }
            set
            {
                switch (value)
                {
                    case HashState.Waiting:
                        this.Hash = "正在排队...";
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
            set
            {
                switch (value)
                {
                    case HashResult.Canceled:
                        this.Hash = "任务已被取消...";
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
            set
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
            set
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
            set
            {
                this.SetPropNotify(ref this._modelDetails, value);
            }
        }

        public string DurationofTask
        {
            get
            {
                return this.durationofTask;
            }
            set
            {
                this.SetPropNotify(ref this.durationofTask, value);
            }
        }

        public ICommand CopyModelHashValueCmd
        {
            get
            {
                if (this.copyModelHashValueCmd is null)
                {
                    this.copyModelHashValueCmd =
                        new RelayCommand(this.CopyViewModelHashValueAction);
                }
                return this.copyModelHashValueCmd;
            }
        }

        public ICommand CopyFileFullPathCmd
        {
            get
            {
                if (this.copyFileFullPathCmd is null)
                {
                    this.copyFileFullPathCmd =
                        new RelayCommand(this.CopyFileFullPathAction);
                }
                return this.copyFileFullPathCmd;
            }
        }

        public ICommand OpenFolderSelectItemCmd
        {
            get
            {
                if (this.openFolderSelectItemCmd is null)
                {
                    this.openFolderSelectItemCmd =
                        new RelayCommand(this.OpenFolderSelectItemAction);
                }
                return this.openFolderSelectItemCmd;
            }
        }

        public ICommand OpenModelFilePathCmd
        {
            get
            {
                if (this.openModelFilePathCmd is null)
                {
                    this.openModelFilePathCmd =
                        new RelayCommand(this.OpenModelFilePathAction);
                }
                return this.openModelFilePathCmd;
            }
        }

        public ICommand OpenFilePropertiesCmd
        {
            get
            {
                if (this.openFilePropertiesCmd is null)
                {
                    this.openFilePropertiesCmd =
                        new RelayCommand(this.OpenFilePropertiesAction);
                }
                return this.openFilePropertiesCmd;
            }
        }

        private void CopyViewModelHashValueAction(object param)
        {
            Clipboard.SetText(this.Hash);
        }

        private void CopyFileFullPathAction(object param)
        {
            Clipboard.SetText(this.FileInfo.FullName);
        }

        private void OpenFolderSelectItemAction(object param)
        {
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            if (File.Exists(this.FileInfo.FullName))
            {
                CommonUtils.OpenFolderAndSelectItem(this.FileInfo.FullName);
            }
            else
            {
                MessageBox.Show(MainWindow.This, $"文件不存在：\n{this.FileInfo.FullName}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenModelFilePathAction(object param)
        {
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            if (File.Exists(this.FileInfo.FullName))
            {
                NativeFunctions.ShellExecuteW(
                MainWindow.WndHandle,
                "open",
                this.FileInfo.FullName,
                null,
                System.IO.Path.GetDirectoryName(this.FileInfo.FullName),
                ShowCmds.SW_SHOWNORMAL);
            }
            else
            {
                MessageBox.Show(MainWindow.This, $"文件不存在：\n{this.FileInfo.FullName}",
                     "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFilePropertiesAction(object param)
        {
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            if (File.Exists(this.FileInfo.FullName))
            {
                var shellExecuteInfo = new SHELLEXECUTEINFOW();
                shellExecuteInfo.cbSize = Marshal.SizeOf(shellExecuteInfo);
                shellExecuteInfo.fMask = SEMaskFlags.SEE_MASK_INVOKEIDLIST;
                shellExecuteInfo.hwnd = MainWindow.WndHandle;
                shellExecuteInfo.lpVerb = "properties";
                shellExecuteInfo.lpFile = this.FileInfo.FullName;
                shellExecuteInfo.lpDirectory = this.FileInfo.DirectoryName;
                shellExecuteInfo.nShow = ShowCmds.SW_SHOWNORMAL;
                NativeFunctions.ShellExecuteExW(ref shellExecuteInfo);
            }
            else
            {
                MessageBox.Show(MainWindow.This, $"文件不存在：\n{this.FileInfo.FullName}",
                     "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HashViewModelCancelled()
        {
            if (this.IsSucceeded)
            {
                return;
            }
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
            {
                return;
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            algoType = Settings.Current.SelectedAlgo;
            AppDispatcher.Invoke(() =>
            {
                this.HashName = algoType;
                this.State = HashState.Running;
            });
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
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
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
                using (FileStream fs = File.OpenRead(this.FileInfo.FullName))
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
                            this.pauseManualResetEvent.WaitOne();
                        }
                        algoObject.TransformFinalBlock(buffer, 0, 0);
                        string hashStr = string.Empty;
                        switch (Settings.Current.SelectedOutputType)
                        {
                            case OutputType.BinaryUpper:
                                hashStr = BitConverter.ToString(algoObject.Hash).Replace("-", "");
                                break;
                            case OutputType.BinaryLower:
                                hashStr = BitConverter.ToString(algoObject.Hash).Replace("-", "").ToLower();
                                break;
                            case OutputType.BASE64:
                                hashStr = Convert.ToBase64String(algoObject.Hash);
                                break;
                        }
                        AppDispatcher.Invoke(() =>
                        {
                            this.Export = true;
                            this.Hash = hashStr;
                            this.FileSize = fs.Length;
                        });
                        if (this.expectedHash != null)
                        {
                            CmpRes result;
                            if (this.expectedHash == string.Empty)
                            {
                                result = CmpRes.Uncertain;
                            }
                            else if (hashStr.Equals(
                                this.expectedHash, StringComparison.OrdinalIgnoreCase))
                            {
                                result = CmpRes.Matched;
                            }
                            else
                            {
                                result = CmpRes.Mismatch;
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
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
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
                using (FileStream fs = File.OpenRead(this.FileInfo.FullName))
                {
                    AppDispatcher.Invoke(() =>
                    {
                        this.Progress = 0L;
                        this.ProgressTotal = fileSize = fs.Length;
                    });
                    int readedSize = 0;
                    Sha224Digest algoObject = new Sha224Digest();
                    byte[] buffer = new byte[blockSize];
                    while (true)
                    {
                        if (this.token.IsCancellationRequested)
                        {
                            goto TaskRunningEnds;
                        }
                        readedSize = fs.Read(buffer, 0, blockSize);
                        if (readedSize <= 0)
                        {
                            break;
                        }
                        AppDispatcher.Invoke(() => { this.Progress += readedSize; });
                        algoObject.BlockUpdate(buffer, 0, readedSize);
                        this.pauseManualResetEvent.WaitOne();
                    }
                    int outLength = algoObject.DoFinal(buffer, 0);
                    string hashStr = string.Empty;
                    switch (Settings.Current.SelectedOutputType)
                    {
                        case OutputType.BinaryUpper:
                            hashStr = BitConverter.ToString(buffer, 0, outLength).Replace("-", "");
                            break;
                        case OutputType.BinaryLower:
                            hashStr = BitConverter.ToString(buffer, 0, outLength).Replace("-", "").ToLower();
                            break;
                        case OutputType.BASE64:
                            hashStr = Convert.ToBase64String(buffer, 0, outLength);
                            break;
                    }
                    AppDispatcher.Invoke(() =>
                    {
                        this.Export = true;
                        this.Hash = hashStr;
                        this.FileSize = fs.Length;
                    });
                    if (this.expectedHash != null)
                    {
                        CmpRes result;
                        if (this.expectedHash == string.Empty)
                        {
                            result = CmpRes.Uncertain;
                        }
                        else if (hashStr.Equals(
                            this.expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            result = CmpRes.Matched;
                        }
                        else
                        {
                            result = CmpRes.Mismatch;
                        }
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
            lock (this.manipulationLock)
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
            lock (this.manipulationLock)
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
        }

        public void ShutdownModel()
        {
            lock (this.manipulationLock)
            {
                if (this.State == HashState.Finished)
                {
                    return;
                }
                this.pauseManualResetEvent.Set();
                if (this.tokenSource != null &&
                    !this.tokenSource.IsCancellationRequested)
                {
                    this.tokenSource.Cancel();
                }
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
            {
                this.tokenSource = new CancellationTokenSource();
            }
            this.token = this.tokenSource.Token;
            this.token.Register(this.HashViewModelCancelled);
        }
    }
}
