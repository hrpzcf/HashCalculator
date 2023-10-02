using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace HashCalculator
{
    internal static class Settings
    {
        private static readonly XmlSerializer serializer =
            new XmlSerializer(typeof(SettingsViewModel));
        private static readonly string configBaseDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly DirectoryInfo ConfigDir =
            new DirectoryInfo(Path.Combine(configBaseDataPath, "HashCalculator"));
        private static readonly string configFile = Path.Combine(ConfigDir.FullName, "settings.xml");
        public static readonly string libDir = Path.Combine(ConfigDir.FullName, "Library");
        private static readonly string libXxHashFilePath = Path.Combine(libDir, "xxhash.dll");

        public static string[] StartupArgs { get; set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        public static bool SaveSettings()
        {
            try
            {
                if (!ConfigDir.Exists)
                {
                    ConfigDir.Create();
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
            return executeResult;
        }

        public static void SetProcessEnvVar()
        {
            Environment.SetEnvironmentVariable("PATH", libDir);
        }

        public static string ExtractXxHashDll(bool force)
        {
            if (force || !File.Exists(libXxHashFilePath) || Current.PreviousVer != Info.Ver)
            {
                if (Current.PreviousVer != Info.Ver)
                {
                    Current.PreviousVer = Info.Ver;
                }
                try
                {
                    if (!Directory.Exists(libDir))
                    {
                        Directory.CreateDirectory(libDir);
                    }
                    string name = Environment.Is64BitProcess ? "xxhash64.dll" : "xxhash32.dll";
                    if (AppLoading.ExecutingAsmb.GetManifestResourceStream(
                        $"HashCalculator.HashAlgos.XxHashDll.{name}") is Stream stream)
                    {
                        using (stream)
                        {
                            byte[] dllBuffer = new byte[stream.Length];
                            stream.Read(dllBuffer, 0, dllBuffer.Length);
                            using (FileStream fs = File.OpenWrite(Path.Combine(libDir, libXxHashFilePath)))
                            {
                                fs.Write(dllBuffer, 0, dllBuffer.Length);
                            }
                        }
                    }
                    else
                    {
                        return $"没有名为 [{name}] 的内嵌资源";
                    }
                }
                catch (Exception ex) { return ex.Message; }
            }
            return default(string);
        }
    }
}
