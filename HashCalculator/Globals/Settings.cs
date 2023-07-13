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
        private static readonly string appBaseDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly DirectoryInfo ConfigDir =
            new DirectoryInfo(Path.Combine(appBaseDataPath, "HashCalculator"));
        private static readonly string configFile = Path.Combine(ConfigDir.FullName, "settings.xml");

        // TODO 单实例模式下，再次启动则把原窗口切换到前台
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
    }
}
