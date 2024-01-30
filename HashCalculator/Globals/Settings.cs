using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace HashCalculator
{
    internal static class Settings
    {
        private const string resPrefix = "HashCalculator.HashAlgos.AlgoDlls";
        private static readonly string configBaseDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly DirectoryInfo ConfigDir =
            new DirectoryInfo(Path.Combine(configBaseDataPath, "HashCalculator"));
        private static readonly string configFile = Path.Combine(ConfigDir.FullName, "settings.xml");
        private static readonly string configFileJson = Path.Combine(ConfigDir.FullName, "settings.json");
        public static readonly string MenuConfigFile = Path.Combine(ConfigDir.FullName, "menus.json");
        public static readonly string MenuConfigUnicode = Path.Combine(ConfigDir.FullName, "menus_unicode.json");
        public static readonly string LibraryDir = Path.Combine(ConfigDir.FullName, "Library");

        public static string[] StartupArgs { get; set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        public static bool SaveSettings()
        {
            try
            {
                if (!Directory.Exists(ConfigDir.FullName))
                {
                    ConfigDir.Create();
                }
                else if (File.Exists(configFile))
                {
                    try
                    {
                        File.Delete(configFile);
                    }
                    catch { }
                }
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                using (StreamWriter sw = new StreamWriter(configFileJson))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(sw))
                {
#if DEBUG
                    jsonTextWriter.Formatting = Formatting.Indented;
                    jsonTextWriter.Indentation = 4;
#endif
                    jsonSerializer.Serialize(jsonTextWriter, Current, typeof(SettingsViewModel));
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return false;
        }

        /// <summary>
        /// 只有在窗口加载前调用才有效，因为部分窗口 xaml 内静态绑定 Settings.Current
        /// </summary>
        public static bool LoadSettings()
        {
            bool settingsModelLoaded = false;
            try
            {
                if (!File.Exists(configFileJson) && File.Exists(configFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SettingsViewModel));
                    using (Stream fs = new FileStream(configFile, FileMode.Open, FileAccess.Read))
                    {
                        if (serializer.Deserialize(fs) is SettingsViewModel model)
                        {
                            Current = model;
                            Current.OnSettingsViewModelDeserialized();
                            settingsModelLoaded = true;
                        }
                    }
                }
                else if (File.Exists(configFileJson))
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    using (StreamReader sr = new StreamReader(configFileJson))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        if (jsonSerializer.Deserialize<SettingsViewModel>(jsonTextReader) is SettingsViewModel model)
                        {
                            Current = model;
                            settingsModelLoaded = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (!settingsModelLoaded)
            {
                Current.ResetTemplatesForExport();
            }
            return settingsModelLoaded;
        }

        public static void SetProcessEnvVar()
        {
            Environment.SetEnvironmentVariable("PATH", LibraryDir);
        }

        private static string ExtractFile(string fname, bool force, bool dll = true)
        {
            string userDllPath = Path.Combine(LibraryDir, fname);
            if (force || !File.Exists(userDllPath))
            {
                try
                {
                    if (!Directory.Exists(LibraryDir))
                    {
                        Directory.CreateDirectory(LibraryDir);
                    }
                    string resPath;
                    if (!dll)
                    {
                        resPath = string.Format("{0}.{1}", resPrefix, fname);
                    }
                    else
                    {
                        resPath = string.Format("{0}.{1}{2}.dll", resPrefix,
                            Path.GetFileNameWithoutExtension(fname),
                            Environment.Is64BitProcess ? "64" : "32");
                    }
                    if (AppLoading.ExecutingAsmb.GetManifestResourceStream(resPath) is Stream stream)
                    {
                        using (stream)
                        {
                            byte[] dllBuffer = new byte[stream.Length];
                            stream.Read(dllBuffer, 0, dllBuffer.Length);
                            using (FileStream fileStream = File.Create(userDllPath))
                            {
                                fileStream.Write(dllBuffer, 0, dllBuffer.Length);
                            }
                        }
                    }
                    else
                    {
                        return $"内嵌资源不存在： {resPath}";
                    }
                }
                catch (Exception exception) { return exception.Message; }
            }
            return string.Empty;
        }

        private static string ExtractHashAlgDll(bool force)
        {
            return ExtractFile(Embedded.HashAlgs, Current.PreviousVer != Info.Ver || force);
        }

        public static string ExtractEmbeddedAlgoDlls(bool force)
        {
            if (Current.PreviousVer != Info.Ver)
            {
                try
                {
                    foreach (string path in Directory.GetFiles(LibraryDir, "*",
                        SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception) { }
            }
            string message = "\n".Join(
                ExtractFile(Embedded.Readme, Current.PreviousVer != Info.Ver || force, false),
                ExtractHashAlgDll(force));
            if (string.IsNullOrEmpty(message))
            {
                Current.PreviousVer = Info.Ver;
            }
            return message;
        }
    }
}
