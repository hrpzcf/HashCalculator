using System;
using System.IO;
using System.Security.Cryptography;

namespace SumGenerator
{
    internal static class SerialGenerator
    {
        private static int serialNum = 0;

        public static int GetSerial()
        {
            return ++serialNum;
        }

        public static void SerialBack()
        {
            --serialNum;
        }

        public static void ResetSerial()
        {
            serialNum = 0;
        }
    }

    internal class ShaModel
    {
        private static readonly SHA256 sha256 = SHA256.Create();

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
            this.Sha256 = this.Calculate();
        }

        public int Serial { get; set; }

        public FileInfo Path { get; set; }

        public string Name { get; set; }

        public string Sha256 { get; set; }

        public bool ToExport { get; set; }

        private string Calculate()
        {
            if (!this.Path.Exists)
            {
                return "此文件文件不存在或者因权限问题无法访问。";
            }
            try
            {
                using (FileStream fs = File.OpenRead(this.Path.FullName))
                {
                    byte[] sha256bytes = sha256.ComputeHash(fs);
                    return BitConverter.ToString(sha256bytes).Replace("-", "").ToLower();
                }
            }
            catch
            {
                return "打开文件或计算 SHA256 值失败。";
            }
        }
    }
}
