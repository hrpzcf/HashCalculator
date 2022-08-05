using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;

namespace HashCalculator
{
    internal enum CmpRes
    {
        NoResult,
        Matched,
        Mismatch,
    }

    internal class ShaModel : DependencyObject
    {
        private bool completed = false;
        private static readonly Semaphore threadLimit = new Semaphore(4, 4);
        private static readonly DependencyProperty Sha256Property = DependencyProperty.Register(
            "Sha256",
            typeof(string),
            typeof(ShaModel),
            new PropertyMetadata(string.Empty)
        );
        private static readonly DependencyProperty CmpResultProperty = DependencyProperty.Register(
            "CmpResult",
            typeof(CmpRes),
            typeof(ShaModel),
            new PropertyMetadata(CmpRes.NoResult)
        );

        public ShaModel(int serial, FileInfo path)
        {
            this.Serial = serial;
            this.Path = path;
            this.Initialize();
        }

        private void Initialize()
        {
            this.Name = this.Path.Name;
            this.ToExport = true;
        }

        public bool Completed
        {
            get { return completed; }
        }

        public int Serial { get; set; }

        public FileInfo Path { get; set; }

        public string Name { get; set; }

        public bool ToExport { get; set; }

        public string Sha256
        {
            get { return (string)this.GetValue(Sha256Property); }
            set { this.SetValue(Sha256Property, value); }
        }

        public CmpRes CmpResult
        {
            get { return (CmpRes)this.GetValue(CmpResultProperty); }
            set { this.SetValue(CmpResultProperty, value); }
        }

        /// <summary>
        /// 在限制线程数量下计算文件的散列值
        /// </summary>
        public void GenerateHashLimited()
        {
            threadLimit.WaitOne();
            this.GenerateHash();
            threadLimit.Release();
        }

        private void GenerateHash()
        {
            if (!this.Path.Exists)
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        this.Sha256 = "非普通文件 / 无法访问";
                    }
                );
                goto BeforeReturn;
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    using (SHA256 sha256 = new SHA256Cng())
                    {
                        string sha256string = BitConverter
                            .ToString(sha256.ComputeHash(fs))
                            .Replace("-", "");
                        Application.Current.Dispatcher.Invoke(
                            () =>
                            {
                                this.Sha256 = sha256string;
                            }
                        );
                        this.completed = true;
                    }
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        this.Sha256 = "无法读取文件 / 计算出错";
                    }
                );
            }
        BeforeReturn:
            CompletionCounter.Increment();
        }
    }
}
