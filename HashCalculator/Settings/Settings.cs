using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Handy = HandyControl;

namespace HashCalculator
{
    internal static class Settings
    {
        private const string hashAlgoDllResPrefix = "HashCalculator.Algorithm.AlgoDlls";

        /// <summary>
        /// 算法的实现库名称 (外置的动态链接库)
        /// </summary>
        public const string HashAlgs = "hashalgs.dll";

        public static string ShellExtensionName { get; } = Environment.Is64BitOperatingSystem ?
            "HashCalculator.dll" : "HashCalculator32.dll";

        public static ConfigPaths ConfigInfo { get; private set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        static Settings()
        {
            ConfigInfo = new ConfigPaths(ConfigLocation.Test, null);
        }

        public static void UpdateConfigurationPaths(ConfigLocation location)
        {
            try
            {
                ConfigPaths newInfo = new ConfigPaths(location, null);
                if (newInfo.ActiveConfigDir.Equals(ConfigInfo.ActiveConfigDir))
                {
                    return;
                }
                ConfigPaths oldInfo = ConfigInfo;
                ConfigInfo = newInfo;
                if (!Directory.Exists(newInfo.ActiveConfigDir))
                {
                    Directory.CreateDirectory(newInfo.ActiveConfigDir);
                }
                if (File.Exists(oldInfo.ActiveConfigFile))
                {
                    if (File.Exists(newInfo.ActiveConfigFile))
                    {
                        File.Delete(newInfo.ActiveConfigFile);
                    }
                    File.Move(oldInfo.ActiveConfigFile, newInfo.ActiveConfigFile);
                }
                // 外壳扩展未安装，可以移动右键菜单配置文件
                // 否则并不能移动右键菜单配置文件，需要在外壳扩展被卸载后触发移动
                if (newInfo.ShellExtensionExists == false)
                {
                    if (File.Exists(oldInfo.MenuConfigFile))
                    {
                        if (File.Exists(newInfo.MenuConfigFile))
                        {
                            File.Delete(newInfo.MenuConfigFile);
                        }
                        File.Move(oldInfo.MenuConfigFile, newInfo.MenuConfigFile);
                    }
                }
            }
            catch
            {
            }
            UpdateDisplayingInformation();
        }

        public static void UpdateShellMenuConfigFilePath(string shellExtFile, bool update = true)
        {
            ConfigInfo.UpdateShellMenuConfigFilePath(shellExtFile);
            if (update)
            {
                UpdateDisplayingInformation();
            }
        }

        public static void UpdateDisplayingInformation(RegBranch branch = RegBranch.UNKNOWN)
        {
            Current.DisplayingActiveConfigDir = ConfigInfo.ActiveConfigDir;
            Current.DisplayingShellExtensionDir = ConfigInfo.ShellExtensionDir;
            if (branch == RegBranch.UNKNOWN)
            {
                branch = ShellExtHelper.GetShellExtLocation();
            }
            switch (branch)
            {
                case RegBranch.HKCU:
                    Current.DisplayingShellInstallationState = "已安装";
                    Current.DisplayingShellInstallationScope = "当前用户";
                    break;
                case RegBranch.HKLM:
                    Current.DisplayingShellInstallationState = "已安装";
                    Current.DisplayingShellInstallationScope = "当前系统";
                    break;
                case RegBranch.BOTH:
                    Current.DisplayingShellInstallationState = "已安装";
                    Current.DisplayingShellInstallationScope = "当前系统和用户";
                    break;
                case RegBranch.NEITHER:
                    Current.DisplayingShellInstallationState = "未安装";
                    Current.DisplayingShellInstallationScope = ShellExtHelper.RunningAsAdmin ? "当前系统" : "当前用户";
                    break;
                default:
                case RegBranch.UNKNOWN:
                    Current.DisplayingShellInstallationState = "无法确定";
                    Current.DisplayingShellInstallationScope = "无法确定";
                    break;
            }
        }

        public static async void MoveConfigFiles(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.LocationForSavingConfigFiles) &&
                sender is SettingsViewModel settingsViewModel &&
                !settingsViewModel.ProcessingShellExtension)
            {
                settingsViewModel.ProcessingShellExtension = true;
                await Task.Run(() =>
                {
                    UpdateConfigurationPaths(settingsViewModel.LocationForSavingConfigFiles);
                });
                settingsViewModel.ProcessingShellExtension = false;
            }
        }

        public static bool SaveSettings()
        {
            try
            {
                if (!Directory.Exists(ConfigInfo.ActiveConfigDir))
                {
                    Directory.CreateDirectory(ConfigInfo.ActiveConfigDir);
                }
                using (StreamWriter sw = new StreamWriter(ConfigInfo.ActiveConfigFile))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(sw))
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Error = (s, e) =>
                        {
                            e.ErrorContext.Handled = true;
                        },
                    };
                    JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
                    jsonSerializer.Serialize(jsonTextWriter, Current, typeof(SettingsViewModel));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Handy.Controls.MessageBox.Show($"无法把配置文件保存到程序目录：{ex.Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private static void DeleteAlgoDllsExceptThatWithinActiveDir()
        {
            if (!ConfigInfo.ActiveConfigDir.Equals(ConfigPaths.ConfigDirExec))
            {
                string unusedDll = Path.Combine(ConfigPaths.ConfigDirExec, HashAlgs);
                if (File.Exists(unusedDll))
                {
                    try
                    {
                        File.Delete(unusedDll);
                    }
                    catch
                    {
                    }
                }
            }
            if (!ConfigInfo.ActiveConfigDir.Equals(ConfigPaths.ConfigDirUser))
            {
                string unusedDll = Path.Combine(ConfigPaths.ConfigDirUser, HashAlgs);
                if (File.Exists(unusedDll))
                {
                    try
                    {
                        File.Delete(unusedDll);
                    }
                    catch
                    {
                    }
                }
            }
            if (!ConfigInfo.ActiveConfigDir.Equals(ConfigPaths.ConfigDirPublicUser))
            {
                string unusedDll = Path.Combine(ConfigPaths.ConfigDirPublicUser, HashAlgs);
                if (File.Exists(unusedDll))
                {
                    try
                    {
                        File.Delete(unusedDll);
                    }
                    catch
                    {
                    }
                }
            }
            if (!ConfigInfo.ActiveConfigDir.Equals(ConfigPaths.ConfigDirProgramData))
            {
                string unusedDll = Path.Combine(ConfigPaths.ConfigDirProgramData, HashAlgs);
                if (File.Exists(unusedDll))
                {
                    try
                    {
                        File.Delete(unusedDll);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 只有在窗口加载前调用才有效，因为部分窗口 xaml 内静态绑定 Settings.Current
        /// </summary>
        public static bool LoadSettings()
        {
            bool settingsViewModelLoaded = false;
            DeleteAlgoDllsExceptThatWithinActiveDir();
            // 删除已弃用的、放置在单独目录的算法动态库
            string oldAlgDll = Path.Combine(ConfigPaths.LibraryDirUser, HashAlgs);
            try
            {
                if (File.Exists(oldAlgDll))
                {
                    File.Delete(oldAlgDll);
                }
                if (Directory.Exists(ConfigPaths.LibraryDirUser))
                {
                    Directory.Delete(ConfigPaths.LibraryDirUser);
                }
            }
            catch
            {
            }
            try
            {
                if (File.Exists(ConfigInfo.ActiveConfigFile))
                {
                    // 读取所有字符串的原因是尽早关闭文件以免影响反序列化导致
                    // SettingsViewModel.LocationForSavingConfigFiles 属性变化
                    // 触发的移动配置文件位置的操作（无法移动还没有关闭的文件）
                    string jContent = File.ReadAllText(ConfigInfo.ActiveConfigFile);
                    using (StringReader sr = new StringReader(jContent))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            Error = (s, e) =>
                            {
                                e.ErrorContext.Handled = true;
                            },
                        };
                        JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
                        jsonSerializer.Populate(jsonTextReader, Current);
                        settingsViewModelLoaded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Handy.Controls.MessageBox.Show($"设置加载失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdateDisplayingInformation();
            if (!settingsViewModelLoaded)
            {
                Current.ResetTemplatesForExport();
                Current.ResetTemplatesForChecklist();
            }
            return settingsViewModelLoaded;
        }

        public static void SetProcessEnvVar()
        {
            Environment.SetEnvironmentVariable("PATH", ConfigInfo.ActiveConfigDir);
        }

        public static string ExtractEmbeddedAlgoDll(bool force)
        {
            string newFileFullPath = Path.Combine(ConfigInfo.ActiveConfigDir, HashAlgs);
            if (force || Current.PreviousVer != Info.Ver || !File.Exists(newFileFullPath))
            {
                try
                {
                    if (!Directory.Exists(ConfigInfo.ActiveConfigDir))
                    {
                        Directory.CreateDirectory(ConfigInfo.ActiveConfigDir);
                    }
                    string resourcePath = string.Format("{0}.{1}{2}.dll",
                        hashAlgoDllResPrefix,
                        Path.GetFileNameWithoutExtension(HashAlgs),
                        Environment.Is64BitProcess ? "64" : "32");
                    using (Stream stream = App.Executing.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            stream.ToNewFile(newFileFullPath);
                        }
                        else
                        {
                            return $"内嵌资源不存在： {resourcePath}";
                        }
                    }
                }
                catch (Exception exception) { return exception.Message; }
            }
            return null;
        }

        public static async Task<string> TestCompatibilityOfShellExt()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string shellExtPath = ShellExtHelper.GetShellExtensionPath();
                    if (!File.Exists(shellExtPath))
                    {
                        return null;
                    }
                    FileVersionInfo verInfo = FileVersionInfo.GetVersionInfo(shellExtPath);
                    Version shellExtVersion = new Version(verInfo.FileVersion);
                    if (shellExtVersion >= Info.MinVerOfCompatibleShellExt && shellExtVersion <= Info.V)
                    {
                        return null;
                    }
                    return $"{Info.Name} {Info.Ver} 与右键菜单扩展模块 {shellExtVersion} 不兼容，为保证右键" +
                        $"菜单与此版本的 {Info.Name} 正确配合，请使用旧版卸载右键菜单，再使用此版本重新安装右键菜单！";
                }
                catch (Exception e)
                {
                    return $"测试 {Info.Name} 右键菜单扩展模块兼容性失败，异常信息：{e.Message}";
                }
            });
        }
    }
}
