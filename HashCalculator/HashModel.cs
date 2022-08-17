using Org.BouncyCastle.Crypto.Digests;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace HashCalculator
{
    internal enum CmpRes
    {
        NoOption,
        NoResult,
        Matched,
        Mismatch,
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
        private bool completed = false;
        private static readonly DependencyProperty ExportProperty = DependencyProperty.Register(
            "Export",
            typeof(bool),
            typeof(HashModel),
            new PropertyMetadata(true)
        );
        private static readonly DependencyProperty HashAlgoProperty = DependencyProperty.Register(
            "Hash",
            typeof(string),
            typeof(HashModel),
            new PropertyMetadata("正在计算...")
        );
        private static readonly DependencyProperty HashNameProperty = DependencyProperty.Register(
            "HashName",
            typeof(AlgoType),
            typeof(HashModel),
            new PropertyMetadata(AlgoType.SHA256)
        );
        private static readonly DependencyProperty CmpResultProperty = DependencyProperty.Register(
            "CmpResult",
            typeof(CmpRes),
            typeof(HashModel),
            new PropertyMetadata(CmpRes.NoOption)
        );

        public HashModel(int serial, FileInfo path)
        {
            this.Serial = serial;
            this.Path = path;
            this.Initialize();
        }

        private void Initialize()
        {
            this.Name = this.Path.Name;
            this.Export = true;
        }

        public int Serial { get; set; }

        public FileInfo Path { get; set; }

        public string Name { get; set; }

        public bool Export
        {
            set { this.SetValue(ExportProperty, value); }
            get { return (bool)this.GetValue(ExportProperty); }
        }

        public bool Completed { get { return this.completed; } }

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

        /// <summary>
        /// 在限制线程数量下计算文件的散列值
        /// </summary>
        public void EnterGenerateUnderLimit()
        {
            Locks.ComputeTaskLock.WaitOne();
            this.ComputeManyHashValue();
            Locks.ComputeTaskLock.Release();
        }

        private void ComputeManyHashValue()
        {
            AlgoType algoType;
            HashAlgorithm hashAlgo;
            lock (Locks.AlgoSelectionLock)
            {
                algoType = Settings.Current.SelectedAlgo;
                Application.Current.Dispatcher.Invoke(() => { this.HashName = algoType; });
            }
            if (!this.Path.Exists)
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "非普通文件 / 无法访问"; this.Export = false; }
                );
                goto BeforeReturn;
            }
            switch (algoType)
            {
                case AlgoType.SHA1:
                    hashAlgo = new SHA1Cng();
                    break;
                case AlgoType.SHA384:
                    hashAlgo = new SHA384Cng();
                    break;
                case AlgoType.SHA512:
                    hashAlgo = new SHA512Cng();
                    break;
                case AlgoType.MD5:
                    hashAlgo = new MD5Cng();
                    break;
                case AlgoType.SHA224:
                    this.UsingBouncyCastleSha224();
                    return;
                //hashAlgo = new BouncyCastSha224();
                //break;
                case AlgoType.SHA256:
                default:
                    hashAlgo = new SHA256Cng();
                    break;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    using (hashAlgo)
                    {
                        hashAlgo.ComputeHash(fs);
                        string hashStr = BitConverter
                        .ToString(hashAlgo.Hash)
                        .Replace("-", "");
                        Application.Current.Dispatcher.Invoke(() => { this.Hash = hashStr; });
                        this.completed = true;
                    }
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "无法读取文件 / 计算出错"; this.Export = false; }
                );
            }
        BeforeReturn:
            CompletionCounter.Increment();
        }

        private void UsingBouncyCastleSha224()
        {
            int blen = 2097152;
            int outBytesLength;
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    Sha224Digest sha224 = new Sha224Digest();
                    int bytesReaded = 0;
                    byte[] buffer = new byte[blen];
                    while ((bytesReaded = fs.Read(buffer, 0, blen)) != 0)
                        sha224.BlockUpdate(buffer, 0, bytesReaded);
                    outBytesLength = sha224.DoFinal(buffer, 0);
                    string hashStr = BitConverter
                        .ToString(buffer, 0, outBytesLength)
                        .Replace("-", "");
                    Application.Current.Dispatcher.Invoke(() => { this.Hash = hashStr; });
                    this.completed = true;
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () => { this.Hash = "无法读取文件 / 计算出错"; this.Export = false; }
                );
            }
            CompletionCounter.Increment();
        }
    }
}
