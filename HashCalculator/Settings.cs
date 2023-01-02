using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using static System.Environment;

namespace HashCalculator
{
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
            set { config = value; /*SaveConfigure();*/ }
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
        private double mainWindowWidth = 800.0;
        private double mainWindowHeight = 600.0;
        private double mainWindowTop = 0.0;
        private double mainWindowLeft = 0.0;
        private string savedDirPath = string.Empty;
        private SimCalc simulCalculate = SimCalc.Four;
        private double settingsWinWidth = 400.0;
        private double settingsWinHeight = 280.0;

        public string SavedDirPath
        {
            get
            {
                if (this.savedDirPath != string.Empty)
                    return this.savedDirPath;
                return GetFolderPath(SpecialFolder.Desktop);
            }
            set
            {
                if (value != null)
                    this.savedDirPath = value;
                else
                    this.savedDirPath = string.Empty;
            }
        }

        public bool RembMainWindowSize { get; set; }

        public AlgoType SelectedAlgo { get; set; }

        public bool MainWindowTopmost { get; set; }

        public double MainWindowWidth { get { return this.mainWindowWidth; } set { this.mainWindowWidth = value; } }

        public double MainWindowHeight { get { return this.mainWindowHeight; } set { this.mainWindowHeight = value; } }

        public SearchPolicy DroppedSearchPolicy { get; set; }

        public SearchPolicy QuickVerificationSearchPolicy { get; set; }

        public bool UseLowercaseHash { get; set; }

        public bool RemMainWindowPosition { get; set; }

        public double MainWindowTop { get { return this.mainWindowTop; } set { this.mainWindowTop = value; } }

        public double MainWindowLeft { get { return this.mainWindowLeft; } set { this.mainWindowLeft = value; } }

        public SimCalc TaskLimit { get { return this.simulCalculate; } set { this.simulCalculate = value; } }

        public double SettingsWinWidth { get { return this.settingsWinWidth; } set { this.settingsWinWidth = value; } }

        public double SettingsWinHeight { get { return this.settingsWinHeight; } set { this.settingsWinHeight = value; } }

        public bool ShowResultText { get; set; }

        public bool RecalculateIncomplete { get; set; }

        public bool NoExportColumn { get; set; }

        public bool NoDurationColumn { get; set; }
    }
}
