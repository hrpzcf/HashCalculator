using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using static System.Environment;

namespace HashCalculator
{
    [Serializable]
    internal enum AlgoType
    {
        SHA256,
        SHA1,
        SHA224,
        SHA384,
        SHA512,
        MD5,
    }

    internal static class Settings
    {
        private static readonly IFormatter formatter = new BinaryFormatter();
        private static Configure config = null;
        private static readonly string appBaseDataPath = GetFolderPath(
            SpecialFolder.LocalApplicationData
        );
        private static readonly DirectoryInfo configDir = new DirectoryInfo(
            Path.Combine(appBaseDataPath, "HashCalculator")
        );
        private static readonly string configFile = Path.Combine(configDir.FullName, "config.bin");

        public static Configure Current
        {
            get
            {
                if (config == null)
                    config = LoadConfigure();
                return config;
            }
            set
            {
                config = value;
                SaveConfigure();
            }
        }

        public static bool SaveConfigure()
        {
            try
            {
                if (!configDir.Exists)
                    configDir.Create();
                if (config == null)
                    config = new Configure();
                using (FileStream fs = File.Create(configFile))
                    formatter.Serialize(fs, config);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置保存失败：\n{ex.Message}", "错误");
                return false;
            }
        }

        private static Configure LoadConfigure()
        {
            if (!File.Exists(configFile))
                return new Configure();
            try
            {
                using (FileStream fs = File.OpenRead(configFile))
                    return formatter.Deserialize(fs) is Configure c ? c : new Configure();
            }
            catch
            {
                return new Configure();
            }
        }
    }

    [Serializable]
    internal sealed class Configure
    {
        private string savedPath = string.Empty;
        private double mainWindowWidth = 800;
        private double mainWindowHeight = 600;

        public string SavedPath
        {
            get
            {
                if (this.savedPath != string.Empty)
                    return this.savedPath;
                return GetFolderPath(SpecialFolder.Desktop);
            }
            set
            {
                if (value != null)
                    this.savedPath = value;
                else
                    this.savedPath = string.Empty;
            }
        }

        public bool RembMainWinSize { get; set; }

        public AlgoType SelectedAlgo { get; set; }

        public bool MainWindowTopmost { get; set; }

        public double MainWindowWidth
        {
            get { return this.mainWindowWidth; }
            set { this.mainWindowWidth = value; }
        }

        public double MainWindowHeight
        {
            get { return this.mainWindowHeight; }
            set { this.mainWindowHeight = value; }
        }

        public int FolderSearchPolicy { get; set; }
    }
}
