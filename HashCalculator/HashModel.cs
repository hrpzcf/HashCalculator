using Org.BouncyCastle.Crypto.Digests;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;

namespace HashCalculator
{
    internal enum CmpRes
    {
        /// <summary>
        /// 没有执行过比较操作
        /// </summary>
        NoResult,

        /// <summary>
        /// 执行过比较操作但没有关联项
        /// </summary>
        Unrelated,

        /// <summary>
        /// 执行过比较操作且已匹配
        /// </summary>
        Matched,

        /// <summary>
        /// 执行过比较操作但不匹配
        /// </summary>
        Mismatch,

        /// <summary>
        /// 执行过比较操作但未能确定是否匹配
        /// </summary>
        Uncertain,
    }

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

    internal class HashModel : DependencyObject
    {
        private static readonly DependencyProperty ExportProperty = DependencyProperty.Register(
            "Export",
            typeof(bool),
            typeof(HashModel),
            new PropertyMetadata(false)
        );
        private static readonly DependencyProperty HashAlgoProperty = DependencyProperty.Register(
            "Hash",
            typeof(string),
            typeof(HashModel),
            new PropertyMetadata("正在排队...")
        );
        private static readonly DependencyProperty HashNameProperty = DependencyProperty.Register(
            "HashName",
            typeof(AlgoType),
            typeof(HashModel),
            new PropertyMetadata(AlgoType.Unknown)
        );
        private static readonly DependencyProperty CmpResultProperty = DependencyProperty.Register(
            "CmpResult",
            typeof(CmpRes),
            typeof(HashModel),
            new PropertyMetadata(CmpRes.NoResult)
        );
        private bool calculationCompleted = false; // 使用此字段是为了 Completed 属性只读
        private readonly string ExpectedHash;
        private readonly bool Useless;
        private readonly CancellationToken CancelToken;
        private const int blockSize = 2097152;

        public HashModel(int serial, ModelArg args)
        {
            this.Serial = serial;
            this.Path = new FileInfo(args.filePath);
            this.Name = this.Path.Name;
            this.ExpectedHash = args.expected?.ToLower();
            this.Useless = args.useless;
            this.CancelToken = args.cancelToken;
        }

        public int Serial { get; set; }

        public FileInfo Path { get; set; }

        public string Name { get; set; }

        public bool Export
        {
            set { this.SetValue(ExportProperty, value); }
            get { return (bool)this.GetValue(ExportProperty); }
        }

        /// <summary>
        /// 如果计算未完成，即使“导出”（Export）被用户勾上也不会被导出。
        /// 因为计算未完成时 Completed 为 false，导出结果时同时验证 Completed 和 Export。
        /// </summary>
        public bool Completed { get { return this.calculationCompleted; } }

        public string Hash
        {
            set { this.SetValue(HashAlgoProperty, value); }
            get { return (string)this.GetValue(HashAlgoProperty); }
        }

        public AlgoType HashName
        {
            set { this.SetValue(HashNameProperty, value); }
            get { return (AlgoType)this.GetValue(HashNameProperty); }
        }

        public CmpRes CmpResult
        {
            set { this.SetValue(CmpResultProperty, value); }
            get { return (CmpRes)this.GetValue(CmpResultProperty); }
        }

        public void CancelledCallback()
        {
            if (this.Completed) return;
            CompletionCounter.Increment();
            Application.Current.Dispatcher.Invoke(() => { this.Hash = "任务已被取消"; });
        }

        /// <summary>
        /// 在限制线程数量下计算文件的哈希值
        /// </summary>
        public void EnterGenerateUnderLimit()
        {
            Locks.HashComputeLock.WaitOne();
            if (this.CancelToken.IsCancellationRequested)
            {
                CompletionCounter.Increment();
                Application.Current.Dispatcher.Invoke(() => { this.Hash = "任务已被取消"; });
                Locks.HashComputeLock.Release();
                return;
            }
            this.ComputeManyHashValue();
            Locks.HashComputeLock.Release();
        }

        private void ComputeManyHashValue()
        {
            AlgoType algoType;
            HashAlgorithm algorithmHash;
            lock (Locks.AlgoSelectionLock)
            {
                algoType = Settings.Current.SelectedAlgo;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.HashName = algoType; this.Hash = "正在计算...";
                });
            }
            switch (algoType)
            {
                case AlgoType.SHA1:
                    algorithmHash = new SHA1Cng();
                    break;
                case AlgoType.SHA384:
                    algorithmHash = new SHA384Cng();
                    break;
                case AlgoType.SHA512:
                    algorithmHash = new SHA512Cng();
                    break;
                case AlgoType.MD5:
                    algorithmHash = new MD5Cng();
                    break;
                case AlgoType.SHA224:
                    this.UsingBouncyCastleSha224();
                    return;
                //hashAlgo = new BouncyCastSha224();
                //break;
                case AlgoType.SHA256:
                default:
                    algorithmHash = new SHA256Cng();
                    break;
            }
            if (this.Useless)
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "在依据所在文件夹找不到依据中列出的文件"; });
                goto TaskFinishing;
            }
            else if (!this.Path.Exists)
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "要计算哈希值的文件不存在或无法访问"; });
                goto TaskFinishing;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    using (algorithmHash)
                    {
                        int readedSize = 0;
                        byte[] buffer = new byte[blockSize];
                        while (true)
                        {
                            if (this.CancelToken.IsCancellationRequested)
                            {
                                Application.Current.Dispatcher.Invoke(
                                    () => { this.Hash = "任务已被取消"; });
                                goto TaskFinishing;
                            }
                            readedSize = fs.Read(buffer, 0, buffer.Length);
                            if (readedSize <= 0) break;
                            algorithmHash.TransformBlock(buffer, 0, readedSize, null, 0);
                        }
                        algorithmHash.TransformFinalBlock(buffer, 0, 0);
                        string hashStr = BitConverter.ToString(algorithmHash.Hash).Replace("-", "");
                        if (Settings.Current.UseLowercaseHash)
                            hashStr = hashStr.ToLower();
                        Application.Current.Dispatcher.Invoke(
                            () => { this.Export = true; this.Hash = hashStr; });
                        if (this.ExpectedHash != null)
                        {
                            CmpRes result;
                            if (hashStr.ToLower() == this.ExpectedHash)
                                result = CmpRes.Matched;
                            else
                                result = CmpRes.Mismatch;
                            Application.Current.Dispatcher.Invoke(() => { this.CmpResult = result; });
                        }
                        this.calculationCompleted = true;
                    }
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "读取要被计算哈希值的文件失败或计算出错"; });
            }
        TaskFinishing:
            CompletionCounter.Increment();
        }

        private void UsingBouncyCastleSha224()
        {
            if (this.Useless)
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "在依据所在文件夹找不到依据中列出的文件"; });
                goto TaskFinishing;
            }
            else if (!this.Path.Exists)
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "要计算哈希值的文件不存在或无法访问"; });
                goto TaskFinishing;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    int readedSize = 0;
                    Sha224Digest algorithmHash = new Sha224Digest();
                    byte[] buffer = new byte[blockSize];
                    while (true)
                    {
                        if (this.CancelToken.IsCancellationRequested)
                        {
                            Application.Current.Dispatcher.Invoke(
                                () => { this.Hash = "任务已被取消"; });
                            goto TaskFinishing;
                        }
                        readedSize = fs.Read(buffer, 0, blockSize);
                        if (readedSize <= 0) break;
                        algorithmHash.BlockUpdate(buffer, 0, readedSize);
                    }
                    int outLength = algorithmHash.DoFinal(buffer, 0);
                    string hashStr = BitConverter.ToString(buffer, 0, outLength).Replace("-", "");
                    if (Settings.Current.UseLowercaseHash)
                        hashStr = hashStr.ToLower();
                    Application.Current.Dispatcher.Invoke(
                        () => { this.Export = true; this.Hash = hashStr; });
                    if (this.ExpectedHash != null)
                    {
                        CmpRes result;
                        if (hashStr.ToLower() == this.ExpectedHash)
                            result = CmpRes.Matched;
                        else
                            result = CmpRes.Mismatch;
                        Application.Current.Dispatcher.Invoke(() => { this.CmpResult = result; });
                    }
                    this.calculationCompleted = true;
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "读取要被计算哈希值的文件失败或计算出错"; });
            }
        TaskFinishing:
            CompletionCounter.Increment();
        }
    }
}
