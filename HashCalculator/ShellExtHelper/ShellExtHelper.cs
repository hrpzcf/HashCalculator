using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace HashCalculator
{
    internal static class ShellExtHelper
    {
        static ShellExtHelper()
        {
            nodeHashCalculatorPath = new RegNode(keyNameHashCalculator)
            {
                Values = new RegValue[]
                {
                    new RegValue("", executablePath, RegistryValueKind.String),
                    new RegValue("Path", executableDir, RegistryValueKind.String)
                }
            };
        }

        private static Exception JoinExceptionMessagesAndGenerateNew(params Exception[] exceptions)
        {
            if (exceptions != null)
            {
                List<int> nonNullExceptionIndexes = new List<int>();
                for (int i = 0; i < exceptions.Length; ++i)
                {
                    if (exceptions[i] != null)
                    {
                        nonNullExceptionIndexes.Add(i);
                    }
                }
                if (nonNullExceptionIndexes.Count == 0)
                {
                    return null;
                }
                else if (nonNullExceptionIndexes.Count == 1)
                {
                    return exceptions[nonNullExceptionIndexes[0]];
                }
                else
                {
                    return new Exception("\n-----\n".Join(nonNullExceptionIndexes.Select(i => exceptions[i].Message)));
                }
            }
            return null;
        }

        public static bool RunningAsAdmin { get; } = CommonUtils.IsRunningAsAdministrator();

        private static Exception ExecuteRegsvr32Command(string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                try
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo("regsvr32", arguments);
                    processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    Process process = new Process();
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return new Exception($"使用 regsvr32 命令注册/反注册外壳扩展失败，错误代码：{process.ExitCode}");
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            }
            return null;
        }

        private static Exception RegisterShellExtDll(string path)
        {
            if (!File.Exists(path))
            {
                return new FileNotFoundException($"扩展模块无法访问或丢失：{path}");
            }
            switch (GetShellExtLocation())
            {
                default:
                case RegBranch.UNKNOWN:
                    return new Exception("无法确定是否已安装外壳扩展模块");
                case RegBranch.HKCU:
                    return new Exception("注册表已有外壳扩展模块信息(用户)，请先卸载右键菜单");
                case RegBranch.HKLM:
                case RegBranch.BOTH:
                    string extra = RunningAsAdmin ? "" : "以管理员身份重新启动程序";
                    return new Exception($"已存在外壳扩展模块信息(系统)，请先{extra}卸载右键菜单");
                case RegBranch.NEITHER:
                    break;
            }
            string arguments = RunningAsAdmin ?
                $"/s \"{path}\"" : $"/s /i:user /n \"{path}\"";
            return ExecuteRegsvr32Command(arguments);
        }

        private static Exception DeregisterShellExtDll(string path)
        {
            if (!File.Exists(path))
            {
                return new FileNotFoundException($"扩展模块无法访问或丢失：{path}");
            }
            string argForHKLM = $"/s /u \"{path}\"";
            string argForHKCU = $"/s /u /i:user /n \"{path}\"";
            Exception exception1 = null;
            Exception exception2 = null;
            RegBranch location = GetShellExtLocation();
            switch (location)
            {
                default:
                case RegBranch.UNKNOWN:
                    return new Exception("无法确定是否已安装外壳扩展模块");
                case RegBranch.HKCU:
                    exception2 = ExecuteRegsvr32Command(argForHKCU);
                    break;
                case RegBranch.HKLM:
                case RegBranch.BOTH:
                    if (!RunningAsAdmin)
                    {
                        return new Exception($"已存在外壳扩展模块信息(系统)，请以管理员身份重新启动程序以卸载右键菜单");
                    }
                    exception1 = ExecuteRegsvr32Command(argForHKLM);
                    if (location == RegBranch.BOTH)
                    {
                        exception2 = ExecuteRegsvr32Command(argForHKCU);
                    }
                    break;
                case RegBranch.NEITHER:
                    return null;
            }
            return JoinExceptionMessagesAndGenerateNew(exception1, exception2);
        }

        private static void TerminateExplorer()
        {
            List<Process> killedExplorerProcessList = new List<Process>();
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception) { }
                    killedExplorerProcessList.Add(process);
                }
            }
            foreach (Process killedExplorerProcess in killedExplorerProcessList)
            {
                try
                {
                    killedExplorerProcess.WaitForExit(3000);
                }
                catch (Exception) { }
            }
        }

        private static Exception DeleteSubKeyTreeUnderRegistryRoot(RegistryKey root)
        {
            Debug.Assert(root != null, "强制删除注册表时给出的注册表根为空");
            try
            {
                root.DeleteSubKeyTree(registryClsIdComputeHash, false);
                root.DeleteSubKeyTree(registryMenuHandlersComputeHashAnyFile, false);
                root.DeleteSubKeyTree(registryMenuHandlersComputeHashDirectory, false);
                root.DeleteSubKeyTree(registryMenuHandlersComputeHashBackground, false);
                root.DeleteSubKeyTree(registryApplications, false);
                root.DeleteSubKeyTree(registryClsIdAsCheckList, false);
                root.DeleteSubKeyTree(registryHcbFile, false);
                root.DeleteSubKeyTree(registryProgIdChecklist, false);
                root.DeleteSubKeyTree(registryMenuHandlersOpenAsChecklistAnyFile, false);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static async Task<Exception> ForceCleanRegistryKeysManually()
        {
            Exception exception1 = await RegDeleteAppPathAsync();
            Exception exception2 = null;
            Exception exception3 = null;
            RegBranch location = GetShellExtLocation();
            switch (location)
            {
                default:
                case RegBranch.UNKNOWN:
                    exception2 = new Exception("无法确定是否已安装外壳扩展模块");
                    break;
                case RegBranch.HKCU:
                    exception2 = DeleteSubKeyTreeUnderRegistryRoot(Registry.CurrentUser);
                    break;
                case RegBranch.HKLM:
                case RegBranch.BOTH:
                    if (!RunningAsAdmin)
                    {
                        return new Exception($"找到外壳扩展模块信息(系统)，请以管理员身份重新启动程序再清理");
                    }
                    exception2 = DeleteSubKeyTreeUnderRegistryRoot(Registry.LocalMachine);
                    if (location == RegBranch.BOTH)
                    {
                        exception3 = DeleteSubKeyTreeUnderRegistryRoot(Registry.CurrentUser);
                    }
                    break;
                case RegBranch.NEITHER:
                    break;
            }
            return JoinExceptionMessagesAndGenerateNew(exception1, exception2, exception3);
        }

        public static async Task<Exception> InstallShellExtension()
        {
            Exception exception = await Task.Run(async () =>
            {
                try
                {
                    if (!Directory.Exists(Settings.ShellExtensionDir))
                    {
                        Directory.CreateDirectory(Settings.ShellExtensionDir);
                    }
                    if (await UninstallShellExtension() is Exception exception1)
                    {
                        return exception1;
                    }
                    if (Loading.Executing.GetManifestResourceStream(embeddedShellExtPath) is Stream manifest)
                    {
                        using (manifest)
                        {
                            manifest.ToNewFile(Settings.ShellExtensionFile);
                        }
                    }
                    else
                    {
                        return new MissingManifestResourceException("内嵌的右键菜单扩展模块资源丢失");
                    }
                    if (RegisterShellExtDll(Settings.ShellExtensionFile) is Exception exception2)
                    {
                        return exception2;
                    }
                    SHELL32.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST,
                        IntPtr.Zero, IntPtr.Zero);
                    Settings.UpdateShellMenuConfigFilePath(Settings.ShellExtensionFile);
                    return await RegUpdateAppPathAsync();
                }
                catch (Exception exception3)
                {
                    return exception3;
                }
            });
            Settings.UpdateDisplayingInformation();
            return exception;
        }

        public static async Task<Exception> UninstallShellExtension()
        {
            Exception exception = await Task.Run(async () =>
            {
                Exception exception1 = await RegDeleteAppPathAsync();
                Exception exception2 = null;
                if (GetShellExtensionPath() is string shellExtensionFile &&
                    (exception2 = DeregisterShellExtDll(shellExtensionFile)) == null)
                {
                    string oldMenuConfigFile = Settings.MenuConfigFile;
                    string oldShellExtensionDir = Path.GetDirectoryName(shellExtensionFile);
                    Settings.UpdateShellMenuConfigFilePath(string.Empty);
                    if (!oldShellExtensionDir.Equals(Settings.ActiveConfigDir))
                    {
                        try
                        {
                            if (File.Exists(Settings.MenuConfigFile))
                            {
                                File.Delete(Settings.MenuConfigFile);
                            }
                            File.Move(oldMenuConfigFile, Settings.MenuConfigFile);
                        }
                        catch
                        {
                        }
                    }
                    try
                    {
                        File.Delete(shellExtensionFile);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        TerminateExplorer();
                        try
                        {
                            File.Delete(shellExtensionFile);
                        }
                        catch
                        {
                        }
                    }
                }
                return JoinExceptionMessagesAndGenerateNew(exception1, exception2);
            });
            if (exception is FileNotFoundException)
            {
                Func<MessageBoxResult> callback = () =>
                {
                    return MessageBox.Show(
                        SettingsPanel.Current,
                        "已安装右键菜单但未找到或无法访问外壳扩展模块，" +
                        "无法通过对模块进行反注册来清理右键菜单注册信息，是否直接删除这些信息？",
                        "提示",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No);
                };
                if (MainWndViewModel.Synchronization.Invoke(callback) == MessageBoxResult.Yes)
                {
                    exception = await ForceCleanRegistryKeysManually();
                }
            }
            SHELL32.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST,
                IntPtr.Zero, IntPtr.Zero);
            Settings.UpdateDisplayingInformation();
            return exception;
        }

        private static async Task<Exception> RegistryWriteNode(RegistryKey root, string subPath, RegNode regNode)
        {
            Debug.Assert(root != null && regNode != null);
            if (!string.IsNullOrEmpty(subPath))
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        using (RegistryKey regKey = root.CreateSubKey(subPath, true))
                        {
                            if (regKey == null)
                            {
                                return new Exception($"无法打开注册表节点：{subPath}");
                            }
                            if (!regKey.WriteNode(regNode))
                            {
                                return new Exception($"写入注册表节点失败：{regNode.Name}");
                            }
                        }
                        return null;
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                });
            }
            return new Exception($"需要打开的注册表节点为空");
        }

        private static async Task<Exception> RegistryDeleteNode(RegistryKey root, string subPath, RegNode regNode)
        {
            Debug.Assert(root != null && regNode != null);
            if (!string.IsNullOrEmpty(subPath))
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        using (RegistryKey regKey = root.OpenSubKey(subPath, true))
                        {
                            if (regKey != null)
                            {
                                regKey.DeleteSubKeyTree(regNode.Name, false);
                                return null;
                            }
                            else
                            {
                                return new Exception($"无法打开注册表节点：{subPath}");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                });
            }
            return new Exception($"需要打开的注册表节点为空");
        }

        private static RegBranch GetLocationOfOneInRegistry(string regPath)
        {
            if (string.IsNullOrEmpty(regPath))
            {
                return RegBranch.UNKNOWN;
            }
            RegistryKey keyInCurrentUserBranch = null;
            RegistryKey keyInLocalMachineBranch = null;
            try
            {
                keyInCurrentUserBranch = Registry.CurrentUser.OpenSubKey(regPath, false);
                keyInLocalMachineBranch = Registry.LocalMachine.OpenSubKey(regPath, false);
                if (keyInCurrentUserBranch != null && keyInLocalMachineBranch != null)
                {
                    return RegBranch.BOTH;
                }
                else if (keyInCurrentUserBranch != null)
                {
                    return RegBranch.HKCU;
                }
                else if (keyInLocalMachineBranch != null)
                {
                    return RegBranch.HKLM;
                }
                else
                {
                    return RegBranch.NEITHER;
                }
            }
            catch (SecurityException)
            {
                return RegBranch.UNKNOWN;
            }
            finally
            {
                keyInCurrentUserBranch?.Close();
                keyInLocalMachineBranch?.Close();
            }
        }

        public static RegBranch GetShellExtLocation()
        {
            return GetLocationOfOneInRegistry($"{registryCLSID}\\{Info.RegGuidComputeHash}");
        }

        public static RegBranch GetExecutableLocation()
        {
            return GetLocationOfOneInRegistry($"{registryAppPaths}\\{keyNameHashCalculator}");
        }

        public static async Task<Exception> RegUpdateAppPathAsync()
        {
            Exception exception1 = null;
            Exception exception2 = null;
            RegBranch location = GetExecutableLocation();
            switch (location)
            {
                default:
                case RegBranch.UNKNOWN:
                    return new Exception("无法确定注册表项 HashCalculator.exe 位置");
                case RegBranch.HKCU:
                    exception1 = await RegistryWriteNode(Registry.CurrentUser, registryAppPaths, nodeHashCalculatorPath);
                    break;
                case RegBranch.HKLM:
                case RegBranch.BOTH:
                    if (!RunningAsAdmin)
                    {
                        return new Exception("请以管理员身份重新启动程序再尝试更新程序路径");
                    }
                    exception2 = await RegistryWriteNode(Registry.LocalMachine, registryAppPaths, nodeHashCalculatorPath);
                    if (location == RegBranch.BOTH)
                    {
                        exception1 = await RegistryWriteNode(Registry.CurrentUser, registryAppPaths, nodeHashCalculatorPath);
                    }
                    break;
                case RegBranch.NEITHER:
                    RegistryKey root = RunningAsAdmin ? Registry.LocalMachine : Registry.CurrentUser;
                    exception1 = await RegistryWriteNode(root, registryAppPaths, nodeHashCalculatorPath);
                    break;
            }
            return JoinExceptionMessagesAndGenerateNew(exception1, exception2);
        }

        public static async Task<Exception> RegDeleteAppPathAsync()
        {
            Exception exception1 = null;
            Exception exception2 = null;
            RegBranch location = GetExecutableLocation();
            switch (location)
            {
                default:
                case RegBranch.UNKNOWN:
                    return new Exception("无法确定注册表项 HashCalculator.exe 位置");
                case RegBranch.HKCU:
                    exception1 = await RegistryDeleteNode(Registry.CurrentUser, registryAppPaths, nodeHashCalculatorPath);
                    break;
                case RegBranch.HKLM:
                case RegBranch.BOTH:
                    if (!RunningAsAdmin)
                    {
                        return new Exception("请以管理员身份重新启动程序再尝试清理程序路径");
                    }
                    exception2 = await RegistryDeleteNode(Registry.LocalMachine, registryAppPaths, nodeHashCalculatorPath);
                    if (location == RegBranch.BOTH)
                    {
                        exception1 = await RegistryDeleteNode(Registry.CurrentUser, registryAppPaths, nodeHashCalculatorPath);
                    }
                    break;
                case RegBranch.NEITHER:
                    return null;
            }
            return JoinExceptionMessagesAndGenerateNew(exception1, exception2);
        }

        public static string GetShellExtensionPath()
        {
            try
            {
                using (RegistryKey subKey = Registry.ClassesRoot.OpenSubKey(registryInprocServer32, false))
                {
                    if (subKey?.GetValue(string.Empty) is string extensionPath && !string.IsNullOrEmpty(extensionPath))
                    {
                        return extensionPath;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private const string keyNameHashCalculator = "HashCalculator.exe";
        private const string registryClasses = "SOFTWARE\\Classes";
        private const string registryAppPaths = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths";
        private static readonly string registryCLSID = $"{registryClasses}\\CLSID";

        // 外壳扩展模块 ComputeHash 部分写入的注册表路径
        private static readonly string registryClsIdComputeHash = $"{registryCLSID}\\{Info.RegGuidComputeHash}";
        private static readonly string registryMenuHandlersComputeHash =
            $"{registryClasses}\\{{0}}\\shellex\\ContextMenuHandlers\\HashCalculator";
        private static readonly string registryMenuHandlersComputeHashAnyFile = string.Format(
            registryMenuHandlersComputeHash, "*");
        private static readonly string registryMenuHandlersComputeHashDirectory = string.Format(
            registryMenuHandlersComputeHash, "Directory");
        private static readonly string registryMenuHandlersComputeHashBackground = string.Format(
            registryMenuHandlersComputeHash, "Directory\\Background");

        // 外壳扩展模块 Open As CheckList 部分写入的注册表路径
        private static readonly string registryClsIdAsCheckList = $"{registryCLSID}\\{Info.RegGuidAsCheckList}";
        private static readonly string registryProgIdChecklist = $"{registryClasses}\\HashCalculator.Checklist";
        private static readonly string registryApplications = $"{registryClasses}\\Applications\\HashCalculator.exe";
        private static readonly string registryHcbFile = $"{registryClasses}\\.hcb";
        private static readonly string registryMenuHandlersOpenAsChecklistAnyFile = $"{registryClasses}\\*\\shellex\\" +
            "ContextMenuHandlers\\HashCalculator OpenAsChecklist";

        // 这个注册表路径直接拼在 HKEY_CLASSES_ROOT 后即可
        private static readonly string registryInprocServer32 = $"CLSID\\{Info.RegGuidComputeHash}\\InprocServer32";

        private static readonly RegNode nodeHashCalculatorPath;
        private static readonly string executablePath = Assembly.GetExecutingAssembly().Location;
        private static readonly string executableDir = Path.GetDirectoryName(executablePath);
        private static readonly string embeddedShellExtPath = $"HashCalculator.ShellExt.{Settings.ShellExtensionName}";
    }
}
