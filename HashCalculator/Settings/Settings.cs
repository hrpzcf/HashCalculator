using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator
{
    internal static class Settings
    {
        /// <summary>
        /// 算法的实现库名称 (外置的动态链接库)
        /// </summary>
        public const string HashAlgs = "hashalgs.dll";

        public static string ShellExtensionName { get; } = Environment.Is64BitOperatingSystem
            ? "HashCalculator.dll" : "HashCalculator32.dll";

        public static ConfigPaths ConfigInfo { get; private set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

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
                    Current.DisplayingShellInstallationState = "已经安装";
                    Current.DisplayingShellInstallationScope = "当前用户";
                    break;
                case RegBranch.HKLM:
                    Current.DisplayingShellInstallationState = "已经安装";
                    Current.DisplayingShellInstallationScope = "当前系统";
                    break;
                case RegBranch.BOTH:
                    Current.DisplayingShellInstallationState = "已经安装";
                    Current.DisplayingShellInstallationScope = "当前系统和用户";
                    break;
                case RegBranch.NEITHER:
                    Current.DisplayingShellInstallationState = "没有安装";
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
                {
                    sw.Write(JsonSerializer.Serialize(Current, JsonOptions));
                }
                return true;
            }
            catch (Exception ex)
            {
                NotificationSender.ShowMessageBox("错误", $"设置保存失败：{ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 当前 hashalgs.dll 随发布配置以 None 方式复制到 exe 所在目录，<br/>
        /// 单文件发布时由 PublishSingleFile 打包进 exe 并在运行时解压加载。<br/>
        /// 因此 exe 所在目录下的 hashalgs.dll 是程序运行所必需的，不能删除。<br/>
        /// 仅清理之前版本遗留到其他配置目录或 Library 目录的 hashalgs.dll 文件。
        /// </summary>
        private static void DeleteTheAlgDllsThatAreNoLongerInUse()
        {
            foreach (string configPath in ConfigPaths.ConfigDirectoryPaths)
            {
                // 跳过 exe 所在目录下的 hashalgs.dll，该文件是复制来的必需文件
                if (configPath.Equals(ConfigPaths.ConfigDirExec,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string unusedAlgDllPath = Path.Combine(configPath, HashAlgs);
                if (File.Exists(unusedAlgDllPath))
                {
                    try
                    {
                        File.Delete(unusedAlgDllPath);
                    }
                    catch { }
                }
            }
            // 删除已弃用的、放置在单独目录的算法动态库
            string oldAlgDll = Path.Combine(ConfigPaths.LibraryDirUser, HashAlgs);
            try
            {
                if (File.Exists(oldAlgDll))
                {
                    File.Delete(oldAlgDll);
                    Directory.Delete(ConfigPaths.LibraryDirUser);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 只有在窗口加载前调用才有效，因为部分窗口 xaml 内静态绑定 Settings.Current
        /// </summary>
        public static bool LoadSettings()
        {
            bool settingsViewModelLoaded = false;
            try
            {
                if (File.Exists(ConfigInfo.ActiveConfigFile))
                {
                    // 读取所有字符串的原因是尽早关闭文件以免影响反序列化导致
                    // SettingsViewModel.LocationForSavingConfigFiles 属性变化触发的移
                    // 动配置文件位置的操作（无法移动还没有关闭的文件）
                    string jContent = File.ReadAllText(ConfigInfo.ActiveConfigFile);
                    try
                    {
                        SettingsViewModel model =
                            JsonSerializer.Deserialize<SettingsViewModel>(jContent, JsonOptions);
                        Current.CopyFromOther(model);
                        settingsViewModelLoaded = model != null;
                    }
                    catch (JsonException)
                    {
                        // 配置内容损坏（如字段类型不匹配），整体回退默认设置
                        settingsViewModelLoaded = false;
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationSender.ShowMessageBox("错误", $"设置加载失败：{ex.Message}");
            }
            UpdateDisplayingInformation();
            if (!settingsViewModelLoaded)
            {
                Current.ResetTemplatesForExport();
                Current.ResetTemplatesForChecklist();
            }
            DeleteTheAlgDllsThatAreNoLongerInUse();
            return settingsViewModelLoaded;
        }

        public static Task<string> TestCompatibilityOfShellExt()
        {
            return Task.Run(() =>
            {
                try
                {
                    string shellExtPath = ShellExtHelper.GetShellExtensionPath();
                    if (!File.Exists(shellExtPath))
                    {
                        return null;
                    }
                    FileVersionInfo fileVer = FileVersionInfo.GetVersionInfo(shellExtPath);
                    Version shellExtVer = new Version(fileVer.FileVersion ?? "0.0.0");
                    // 兼容的 Shell 扩展版本包含下限但不包含上限
                    if (shellExtVer < Info.LowerLimitOfShellExtVersion || shellExtVer >= Info.UpperLimitOfShellExtVersion)
                    {
                        return $"{Info.Title} v{Info.Ver} 可能与它的右键菜单扩展模块 " +
                            $"v{shellExtVer} 不兼容，为保证右键菜单正常工作，请重新安装右键菜单！";
                    }
                }
                catch (Exception e)
                {
                    return $"检查 {Info.Title} 右键菜单扩展模块兼容性失败，异常信息：{e.Message}";
                }
                return default(string);
            });
        }
    }
}
