using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace HashCalculator
{
    [Verb("compute")]
    internal class ComputeHash
    {
        [Option("algo")]
        public string Algo { get; set; }

        [Value(0)]
        public IEnumerable<string> FilePaths { get; set; }
    }

    [Verb("verify")]
    internal class VerifyHash
    {
        [Option("algo", Default = -1)]
        public int AlgoEnum { get; set; }

        [Option("basis", Required = true)]
        public int BasisPath { get; set; }
    }

    internal static class Settings
    {
        private static readonly int maxAlgoEnumInt =
            Enum.GetNames(typeof(AlgoType)).Length - 2;
        private static readonly XmlSerializer serializer =
            new XmlSerializer(typeof(SettingsViewModel));
        private static readonly string appBaseDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );
        private static readonly DirectoryInfo configDir = new DirectoryInfo(
            Path.Combine(appBaseDataPath, "HashCalculator")
        );
        private static readonly string configFile =
            Path.Combine(configDir.FullName, "settings.xml");

        public static string[] StartupArgs { get; set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        public static bool SaveSettings()
        {
            try
            {
                if (!configDir.Exists)
                {
                    configDir.Create();
                }
                using (FileStream fs = File.Create(configFile))
                {
                    serializer.Serialize(fs, Current);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置保存失败：\n{ex.Message}", "错误");
                return false;
            }
        }

        /// <summary>
        /// 只有在窗口加载前调用才有效<br/>
        /// 因为部分窗口 xaml 内以 Static 方式绑定 Settings.Current
        /// </summary>
        public static bool LoadSettings()
        {
            bool executeResult = false;
            if (File.Exists(configFile))
            {
                try
                {
                    using (FileStream fs = File.OpenRead(configFile))
                    {
                        if (serializer.Deserialize(fs) is SettingsViewModel model)
                        {
                            Current = model;
                            executeResult = true;
                        }
                    }
                }
                catch (Exception) { }
            }
            if (StartupArgs.Length > 0)
            {
                Parser.Default.ParseArguments<ComputeHash>(StartupArgs).WithParsed(option =>
                {
                    if (option.FilePaths != null)
                    {
                        MainWindow.PathsFromStartupArgs = option.FilePaths;
                    }
                    if (!string.IsNullOrEmpty(option.Algo))
                    {
                        if (int.TryParse(option.Algo, out int algo))
                        {
                            if (algo >= 0 && algo <= maxAlgoEnumInt)
                            {
                                Current.SelectedAlgo = (AlgoType)algo;
                            }
                        }
                        else if (Enum.TryParse(option.Algo.ToUpper(), out AlgoType algoType))
                        {
                            Current.SelectedAlgo = algoType;
                        }
                    }
                });
            }
            return executeResult;
        }
    }
}
