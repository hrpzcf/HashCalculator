using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace HashCalculator
{
    internal static class Settings
    {
        private const string appConfigFileName = "settings.json";
        private const string menuConfigFileName = "menus_unicode.json";
        private const string hashAlgoDllResPrefix = "HashCalculator.Algorithm.AlgoDlls";
        private const string libraryDirName = "Library";
        private const string hashCalculatorDirName = "HashCalculator";

        /// <summary>
        /// 算法的实现库名称 (外置的动态链接库)
        /// </summary>
        public const string HashAlgs = "hashalgs.dll";

        public static string ShellExtensionName { get; } = Environment.Is64BitOperatingSystem ?
            "HashCalculator.dll" : "HashCalculator32.dll";

        public static string[] StartupArgs { get; internal set; }

        public static string ConfigDirExec { get; private set; }

        public static string ConfigDirUser { get; private set; }

        public static string LibraryDirExec { get; private set; }

        public static string LibraryDirUser { get; private set; }

        public static string MenuConfigFile { get; private set; }

        public static string ShellExtensionDir { get; private set; }

        public static string ShellExtensionFile { get; private set; }

        public static string ActiveConfigDir { get; private set; }

        public static string ActiveLibraryDir { get; private set; }

        public static string ActiveConfigFile { get; private set; }

        public static SettingsViewModel Current { get; private set; }
            = new SettingsViewModel();

        static Settings()
        {
            // 配置文件目录 1：位于程序目录
            ConfigDirExec = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // 哈希算法库目录 1：位于配置文件目录 1
            LibraryDirExec = ConfigDirExec;
            // 程序目录下的配置文件完整路径
            string configFileExec = Path.Combine(ConfigDirExec, appConfigFileName);

            // 配置文件目录 2：位于用户目录下的 HashCalculator 目录
            ConfigDirUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                hashCalculatorDirName);
            // 哈希算法库目录 2：位于配置文件目录 2 下的 Library 目录
            LibraryDirUser = Path.Combine(ConfigDirUser, libraryDirName);
            // 用户目录下的配置文件完整路径
            string configFileUser = Path.Combine(ConfigDirUser, appConfigFileName);

            // 决定读取程序目录下（优先）还是用户目录下的配置文件
            if (File.Exists(configFileExec))
            {
                ActiveConfigDir = ConfigDirExec;
                ActiveLibraryDir = LibraryDirExec;
            }
            else if (File.Exists(configFileUser))
            {
                ActiveConfigDir = ConfigDirUser;
                ActiveLibraryDir = LibraryDirUser;
            }
            else
            {
                ActiveConfigDir = ConfigDirExec;
                ActiveLibraryDir = LibraryDirExec;
            }
            // 系统右键菜单的菜单项配置文件
            UpdateShellMenuConfigFilePath(shellExtFile: null, update: false);
            // 当前使用的配置文件的完整路径
            ActiveConfigFile = Path.Combine(ActiveConfigDir, appConfigFileName);
        }

        public static void UpdateConfigurationPaths(ConfigLocation location)
        {
            try
            {
                if (location == ConfigLocation.Unset)
                {
                    return;
                }
                string switchToDir = location == ConfigLocation.UserDir ?
                    ConfigDirUser : ConfigDirExec;
                if (switchToDir.Equals(ActiveConfigDir))
                {
                    return;
                }
                string oldConfigFilePath = ActiveConfigFile;
                ActiveConfigDir = switchToDir;
                ActiveConfigFile = Path.Combine(switchToDir, appConfigFileName);
                if (!Directory.Exists(switchToDir))
                {
                    Directory.CreateDirectory(switchToDir);
                }
                if (File.Exists(oldConfigFilePath))
                {
                    if (File.Exists(ActiveConfigFile))
                    {
                        File.Delete(ActiveConfigFile);
                    }
                    File.Move(oldConfigFilePath, ActiveConfigFile);
                }
                // 外壳扩展路径为 null 说明扩展未安装，可以移动右键菜单配置文件
                // 否则并不能移动右键菜单配置文件，需要在外壳扩展被卸载后触发移动
                if (ShellExtHelper.GetShellExtensionPath() == null)
                {
                    string oldMenuConfigFile = MenuConfigFile;
                    MenuConfigFile = Path.Combine(ActiveConfigDir, menuConfigFileName);
                    ShellExtensionDir = ActiveConfigDir;
                    ShellExtensionFile = Path.Combine(ActiveConfigDir, ShellExtensionName);
                    if (File.Exists(oldMenuConfigFile))
                    {
                        if (File.Exists(MenuConfigFile))
                        {
                            File.Delete(MenuConfigFile);
                        }
                        File.Move(oldMenuConfigFile, MenuConfigFile);
                    }
                }
            }
            catch
            {
            }
            UpdateDisplayingInformation();
        }

        /// <summary>
        /// shellExtFile 为 null：重新从注册表查询；<br/>
        /// shellExtFile 路径不存在文件：根据活动配置文件目录设置属性；<br/>
        /// shellExtFile 路径存在文件：根据此文件路径设置属性。
        /// </summary>
        public static void UpdateShellMenuConfigFilePath(string shellExtFile, bool update = true)
        {
            if (shellExtFile == null)
            {
                shellExtFile = ShellExtHelper.GetShellExtensionPath();
            }
            if (File.Exists(shellExtFile))
            {
                string shellExtDir = Path.GetDirectoryName(shellExtFile);
                ShellExtensionDir = shellExtDir;
                ShellExtensionFile = shellExtFile;
                MenuConfigFile = Path.Combine(shellExtDir, menuConfigFileName);
            }
            else
            {
                ShellExtensionDir = ActiveConfigDir;
                ShellExtensionFile = Path.Combine(ActiveConfigDir, ShellExtensionName);
                MenuConfigFile = Path.Combine(ActiveConfigDir, menuConfigFileName);
            }
            if (update)
            {
                UpdateDisplayingInformation();
            }
        }

        public static void UpdateDisplayingInformation(RegBranch branch = RegBranch.UNKNOWN)
        {
            Current.DisplayingActiveConfigDir = ActiveConfigDir;
            Current.DisplayingShellExtensionDir = ShellExtensionDir;
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
                if (!Directory.Exists(ActiveConfigDir))
                {
                    Directory.CreateDirectory(ActiveConfigDir);
                }
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                using (StreamWriter sw = new StreamWriter(ActiveConfigFile))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(sw))
                {
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
        /// 只有在窗口加载前调用才有效，因为部分窗口 xaml 内静态绑定 Settings.Current
        /// </summary>
        public static bool LoadSettings()
        {
            bool settingsViewModelLoaded = false;
            if (ActiveConfigDir.Equals(ConfigDirExec))
            {
                string unusedDll = Path.Combine(LibraryDirUser, HashAlgs);
                try
                {
                    if (File.Exists(unusedDll))
                    {
                        File.Delete(unusedDll);
                    }
                    if (Directory.Exists(LibraryDirUser))
                    {
                        Directory.Delete(LibraryDirUser);
                    }
                }
                catch
                {
                }
            }
            else if (ActiveConfigDir.Equals(ConfigDirUser))
            {
                string unusedDll = Path.Combine(LibraryDirExec, HashAlgs);
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
            try
            {
                if (File.Exists(ActiveConfigFile))
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    // 读取所有字符串的原因是尽早关闭文件以免影响反序列化导致
                    // SettingsViewModel.LocationForSavingConfigFiles 属性变化
                    // 触发的移动配置文件位置的操作（无法移动还没有关闭的文件）
                    string jContent = File.ReadAllText(ActiveConfigFile);
                    using (StringReader sr = new StringReader(jContent))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        jsonSerializer.Populate(jsonTextReader, Current);
                        settingsViewModelLoaded = true;
                    }
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
            return settingsViewModelLoaded;
        }

        public static void SetProcessEnvVar()
        {
            Environment.SetEnvironmentVariable("PATH", ActiveLibraryDir);
        }

        public static string ExtractEmbeddedAlgoDllFile(bool force)
        {
            string newFileFullPath = Path.Combine(ActiveLibraryDir, HashAlgs);
            if (force || Current.PreviousVer != Info.Ver || !File.Exists(newFileFullPath))
            {
                try
                {
                    if (!Directory.Exists(ActiveLibraryDir))
                    {
                        Directory.CreateDirectory(ActiveLibraryDir);
                    }
                    string resourcePath = string.Format("{0}.{1}{2}.dll",
                        hashAlgoDllResPrefix,
                        Path.GetFileNameWithoutExtension(HashAlgs),
                        Environment.Is64BitProcess ? "64" : "32");
                    using (Stream stream = Loading.Executing.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            stream.ToNewFile(newFileFullPath);
                            Current.PreviousVer = Info.Ver;
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
    }
}
