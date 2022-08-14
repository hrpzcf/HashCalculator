using Org.BouncyCastle.Crypto.Digests;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;

namespace HashCalculator
{
    internal enum CmpRes
    {
        NoResult,
        Matched,
        Mismatch,
    }

    internal class HashModel : DependencyObject
    {
        private bool completed = false;
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
            new PropertyMetadata(CmpRes.NoResult)
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

        public bool Export { get; set; }

        public bool Completed { get { return this.completed; } }

        public string Hash
        {
            get { return (string)this.GetValue(HashAlgoProperty); }
            set { this.SetValue(HashAlgoProperty, value); }
        }

        public AlgoType HashName
        {
            get { return (AlgoType)this.GetValue(HashNameProperty); }
            set { this.SetValue(HashNameProperty, value); }
        }

        public CmpRes CmpResult
        {
            get { return (CmpRes)this.GetValue(CmpResultProperty); }
            set { this.SetValue(CmpResultProperty, value); }
        }

        /// <summary>
        /// 在限制线程数量下计算文件的散列值
        /// </summary>
        public void EnterGenerateUnderLimit()
        {
            Locks.ComputeTaskLock.WaitOne();
            this.GenerateHashValue();
            Locks.ComputeTaskLock.Release();
        }

        private void GenerateHashValue()
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
                Application.Current.Dispatcher.Invoke(() => { this.Hash = "非普通文件 / 无法访问"; });
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
                    this.UsingBouncyCastleSha224Hash();
                    return;
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
                        string hashString = BitConverter
                        .ToString(hashAlgo.Hash)
                        .Replace("-", "");
                        Application.Current.Dispatcher.Invoke(() => { this.Hash = hashString; });
                        this.completed = true;
                    }
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        this.Hash = "无法读取文件 / 计算出错";
                    }
                );
            }
        BeforeReturn:
            CompletionCounter.Increment();
        }

        private void UsingBouncyCastleSha224Hash()
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
                    string hashString = BitConverter
                        .ToString(buffer, 0, outBytesLength)
                        .Replace("-", "");
                    Application.Current.Dispatcher.Invoke(() => { this.Hash = hashString; });
                    this.completed = true;
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        this.Hash = "无法读取文件 / 计算出错";
                    }
                );
            }
            CompletionCounter.Increment();
        }
    }
}
