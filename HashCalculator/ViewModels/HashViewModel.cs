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
        private string durationofTask = string.Empty;
        private string _hashValue = "正在排队...";
        private string _modelDetails = "暂无详情...";
        private bool _exportHash = false;
        private long _fileSize = 0L;
        private long _progress = 0L;
        private long _progressTotal = 0L;
        private AlgoType _hashName = AlgoType.Unknown;
        private CmpRes _cmpResult = CmpRes.NoResult;
        private HashState _currentState = HashState.Waiting;
        private HashResult _currentResult = HashResult.NoResult;
        private RelayCommand copyModelHashValueCmd;
        private RelayCommand copyFileFullPathCmd;
        private RelayCommand openFilePropertiesCmd;
        private RelayCommand openModelFilePathCmd;
        private RelayCommand openFolderSelectItemCmd;
        #endregion

        private readonly string expectedHash;
        private CancellationTokenSource cancellation;
        private readonly bool isDeprecated;
        private const int blockSize = 2097152;
        private static readonly Dispatcher AppDispatcher
            = Application.Current.Dispatcher;
        private readonly ManualResetEvent manualPauseController = new ManualResetEvent(true);
        private readonly object cmpResultLock = new object();
        private readonly object computeOperationLock = new object();
        private readonly object exportOptionLock = new object();

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
                        new RelayCommand(this.CopyModelHashValueAction);
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

        private void CopyModelHashValueAction(object param)
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
                Path.GetDirectoryName(this.FileInfo.FullName),
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

        public void ModelCancelled()
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

        private void ResetHashViewModel()
        {
            this.cancellation = new CancellationTokenSource();
            this.cancellation.Token.Register(this.ModelCancelled);
            this.DurationofTask = string.Empty;
            this.CmpResult = CmpRes.NoResult;
            this.Result = HashResult.NoResult;
            this.State = HashState.Waiting;
            this.Progress = this.ProgressTotal = 0;
            this.HashName = AlgoType.Unknown;
        }

        public bool StartupModel(bool force)
        {
            bool result = false;
            if (Monitor.TryEnter(this.computeOperationLock))
            {
                if (force
                    || this.State == HashState.Waiting
                    || (this.State == HashState.Finished
                    && this.Result != HashResult.Succeeded))
                {
                    this.ResetHashViewModel();
                    this.ModelCapturedEvent?.Invoke(this);
                    result = true;
                }
                Monitor.Exit(this.computeOperationLock);
            }
            return result;
        }

        public void ShutdownModel()
        {
            if (Monitor.TryEnter(this.computeOperationLock))
            {
                this.cancellation?.Cancel();
                this.manualPauseController.Set();
                if (this.State == HashState.Waiting)
                {
                    this.State = HashState.Finished;
                    this.ModelReleasedEvent?.Invoke(this);
                }
                Monitor.Exit(this.computeOperationLock);
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
            Monitor.Enter(this.computeOperationLock);
            if (this.cancellation.IsCancellationRequested)
            {
                Monitor.Exit(this.computeOperationLock);
                return;
            }
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
                    this.Hash = "在依据所在文件夹中找不到此文件";
                });
                goto TaskRunningEnds;
            }
            // 需要调用 FileInfo 的 Refresh 方法才能更新 FileInfo.Exists
            else if (!File.Exists(this.FileInfo.FullName))
            {
                AppDispatcher.Invoke(() =>
                {
                    this.Result = HashResult.Failed;
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
                    this.Result = HashResult.Failed;
                    this.Hash = "读取文件失败或哈希值计算出错";
                });
            }
        TaskRunningEnds:
            stopwatch.Stop();
            string duration = $"{stopwatch.Elapsed.TotalSeconds:f2}";
            AppDispatcher.Invoke(() =>
            {
                this.DurationofTask = duration;
                this.ModelDetails = $"文件名称：{this.Name}\n文件大小：{UnitCvt.FileSizeCvt(this.FileSize)}\n"
                    + $"任务运行时长：{duration}秒";
                this.State = HashState.Finished;
            });
            this.ModelReleasedEvent?.Invoke(this);
            Monitor.Exit(this.computeOperationLock);
        }
    }
}
