﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace HashCalculator
{
    internal static class Settings
    {
        /// <summary>
        /// 算法的实现库名称 (外置的动态链接库)
        /// </summary>
        public const string HashAlgs = "hashalgs.dll";

        public static string ShellExtensionName { get; } = Environment.Is64BitOperatingSystem ?
            "HashCalculator.dll" : "HashCalculator32.dll";

        public static string[] StartupArgs { get; internal set; }

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
                MessageBox.Show($"无法把配置文件保存到程序目录：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return false;
        }

        /// <summary>
        /// 当前 hashalgs.dll 的释放与加载由 Costura.Fody 在临时文件目录完成，<br/>
        /// 之前版本的 HashCalculator 释放到配置文件目录或 Library 目录的 hashalgs.dll 可全部删除。
        /// </summary>
        private static void DeleteTheAlgDllsThatAreNoLongerInUse()
        {
            foreach (string configPath in ConfigPaths.ConfigDirectoryPaths)
            {
                string unusedAlgDllPath = Path.Combine(configPath, HashAlgs);
                if (File.Exists(unusedAlgDllPath))
                {
                    try
                    {
                        File.Delete(unusedAlgDllPath);
                    }
                    catch
                    {
                    }
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
                if (!File.Exists(ConfigInfo.ActiveConfigFile))
                {
                    throw new FileNotFoundException("活动配置文件不存在！");
                }
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
            catch (Exception ex)
            {
                MessageBox.Show($"设置加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                return null;
            });
        }
    }
}
