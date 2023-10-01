using System;
using System.IO;
using System.Reflection;
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
        private static readonly string libDir = Path.Combine(ConfigDir.FullName, "Library");
        private static readonly string libXxHashPath = Path.Combine(libDir, "xxhash.dll");

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

        public static string ExtractXxHashDll(bool fore)
        {
            if (!fore && File.Exists(libXxHashPath))
            {
                return $"文件已存在：{libXxHashPath}";
            }
            if (!Directory.Exists(libDir))
            {
                try
                {
                    Directory.CreateDirectory(libDir);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            string xxHashResName = Environment.Is64BitProcess ? "xxhash64.dll" : "xxhash32.dll";
            try
            {
                if (AppLoading.ExecutingAsmb.GetManifestResourceStream(
                    $"HashCalculator.HashAlgos.XxHashDll.{xxHashResName}") is Stream stream)
                {
                    using (stream)
                    {
                        byte[] dllBuffer = new byte[stream.Length];
                        stream.Read(dllBuffer, 0, dllBuffer.Length);
                        using (FileStream fs = File.OpenWrite(Path.Combine(libDir, libXxHashPath)))
                        {
                            fs.Write(dllBuffer, 0, dllBuffer.Length);
                        }
                    }
                }
                else
                {
                    return $"找不到程序内嵌的资源：{xxHashResName}";
                }
            }
            catch (Exception ex) { return ex.Message; }
            return default(string);
        }
    }
}
