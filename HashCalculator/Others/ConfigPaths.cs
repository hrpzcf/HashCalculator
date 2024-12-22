using System;
using System.IO;

namespace HashCalculator
{
    internal class ConfigPaths
    {
        private const string configFileName = "settings.json";
        private const string menuConfigFileName = "menus_unicode.json";
        private const string libraryDirName = "Library";
        private const string hashCalculatorDirName = "HashCalculator";

        private static string configDirExec = null;
        private static string configDirUser = null;
        private static string configDirPublicUser = null;
        private static string configDirProgramData = null;

        // 实现算法的动态链接库目录（已弃用，改为与配置文件保存位置相同）
        private static string libraryDirUser = null;

        public ConfigPaths(ConfigLocation location, string shellExtFile)
        {
            this.UpdateConfigurationPaths(location);
            this.UpdateShellMenuConfigFilePath(shellExtFile);
        }

        public string ActiveConfigDir { get; private set; }

        public string ActiveConfigFile { get; private set; }

        public string MenuConfigFile { get; private set; }

        public string ShellExtensionDir { get; private set; }

        public string ShellExtensionFile { get; private set; }

        public bool ShellExtensionExists { get; private set; }

        public ConfigLocation Location { get; private set; }

        public static string LibraryDirUser
        {
            get
            {
                if (string.IsNullOrEmpty(libraryDirUser))
                {
                    libraryDirUser = Path.Combine(ConfigDirUser, libraryDirName);
                }
                return libraryDirUser;
            }
        }

        public static string ConfigDirExec
        {
            get
            {
                if (string.IsNullOrEmpty(configDirExec))
                {
                    configDirExec = Path.GetDirectoryName(App.Executing.Location);
                }
                return configDirExec;
            }
        }

        public static string ConfigDirUser
        {
            get
            {
                if (string.IsNullOrEmpty(configDirUser))
                {
                    configDirUser = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData), hashCalculatorDirName);
                }
                return configDirUser;
            }
        }

        public static string ConfigDirPublicUser
        {
            get
            {
                if (string.IsNullOrEmpty(configDirPublicUser))
                {
                    configDirPublicUser = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonDocuments)), hashCalculatorDirName);
                }
                return configDirPublicUser;
            }
        }

        public static string ConfigDirProgramData
        {
            get
            {
                if (string.IsNullOrEmpty(configDirProgramData))
                {
                    configDirProgramData = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData), hashCalculatorDirName);
                }
                return configDirProgramData;
            }
        }

        private void UpdateConfigurationPaths(ConfigLocation location)
        {
            this.Location = location;
            if (location == ConfigLocation.Unset)
            {
                return;
            }
            switch (location)
            {
                case ConfigLocation.Test:
                    string configFileExec = Path.Combine(ConfigDirExec, configFileName);
                    string configFileUser = Path.Combine(ConfigDirUser, configFileName);
                    string configFilePublic = Path.Combine(ConfigDirPublicUser, configFileName);
                    string configFileProgramData = Path.Combine(ConfigDirProgramData, configFileName);
                    if (File.Exists(configFileExec))
                    {
                        this.Location = ConfigLocation.ExecDir;
                        goto case ConfigLocation.ExecDir;
                    }
                    else if (File.Exists(configFileUser))
                    {
                        this.Location = ConfigLocation.UserDir;
                        goto case ConfigLocation.UserDir;
                    }
                    else if (File.Exists(configFilePublic))
                    {
                        this.Location = ConfigLocation.PublicUser;
                        goto case ConfigLocation.PublicUser;
                    }
                    else if (File.Exists(configFileProgramData))
                    {
                        this.Location = ConfigLocation.ProgramData;
                        goto case ConfigLocation.ProgramData;
                    }
                    else
                    {
                        this.Location = ConfigLocation.ExecDir;
                        goto case ConfigLocation.ExecDir;
                    }
                default:
                case ConfigLocation.ExecDir:
                    this.ActiveConfigDir = ConfigDirExec;
                    break;
                case ConfigLocation.UserDir:
                    this.ActiveConfigDir = ConfigDirUser;
                    break;
                case ConfigLocation.PublicUser:
                    this.ActiveConfigDir = ConfigDirPublicUser;
                    break;
                case ConfigLocation.ProgramData:
                    this.ActiveConfigDir = ConfigDirProgramData;
                    break;
            }
            if (string.IsNullOrEmpty(this.ActiveConfigDir))
            {
                this.ActiveConfigDir = Path.GetDirectoryName(App.Executing.Location);
                this.Location = ConfigLocation.ExecDir;
            }
            this.ActiveConfigFile = Path.Combine(this.ActiveConfigDir, configFileName);
        }

        /// <summary>
        /// shellExtFile 为 null：重新从注册表查询；<br/>
        /// shellExtFile 路径不存在文件：根据活动配置文件目录设置属性；<br/>
        /// shellExtFile 路径存在文件：根据此文件路径设置属性。
        /// </summary>
        public void UpdateShellMenuConfigFilePath(string shellExtFile)
        {
            if (shellExtFile == null)
            {
                shellExtFile = ShellExtHelper.GetShellExtensionPath();
            }
            if (File.Exists(shellExtFile))
            {
                string shellExtDir = Path.GetDirectoryName(shellExtFile);
                this.ShellExtensionDir = shellExtDir;
                this.ShellExtensionFile = shellExtFile;
                this.MenuConfigFile = Path.Combine(shellExtDir, menuConfigFileName);
                this.ShellExtensionExists = true;
            }
            else
            {
                this.ShellExtensionDir = this.ActiveConfigDir;
                this.ShellExtensionFile = Path.Combine(this.ActiveConfigDir, Settings.ShellExtensionName);
                this.MenuConfigFile = Path.Combine(this.ActiveConfigDir, menuConfigFileName);
                this.ShellExtensionExists = false;
            }
        }
    }
}
