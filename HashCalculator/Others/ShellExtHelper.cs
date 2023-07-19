using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace HashCalculator
{
    internal static class ShellExtHelper
    {
        static ShellExtHelper()
        {
            basisSuffixNode = new RegNode(basisFileSuffix)
            {
                Values = new RegValue[]
                {
                    new RegValue("", programId, RegistryValueKind.String)
                }
            };
            progIdNode = new RegNode(programId)
            {
                Nodes = new RegNode[]
                {
                    new RegNode("shell")
                    {
                        Nodes = new RegNode[]
                        {
                            new RegNode("open")
                            {
                                Nodes = new RegNode[]
                                {
                                    new RegNode("command")
                                    {
                                        Values = new RegValue[]
                                        {
                                            new RegValue("", $"{executableName} verify -b \"%1\"",
                                                RegistryValueKind.String)
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new RegNode("DefaultIcon")
                    {
                        Values = new RegValue[]
                        {
                            new RegValue("", $"{shellExtensionPath},-203", RegistryValueKind.String)
                        }
                    }
                },
                Values = new RegValue[]
                {
                    new RegValue("FriendlyTypeName", $"@{shellExtensionPath},-107", RegistryValueKind.String)
                }
            };
            applicationNode = new RegNode(executableName)
            {
                Nodes = progIdNode.Nodes,
                Values = progIdNode.Values
            };
            appPathsNode = new RegNode(executableName)
            {
                Values = new RegValue[]
                {
                    new RegValue("", executablePath, RegistryValueKind.String),
                    new RegValue("Path", executableFolder, RegistryValueKind.String)
                }
            };
        }

        private static Exception RegisterShellExtDll(string path, bool register)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "regsvr32";
            startInfo.Arguments = register ?
                $"/s /n /i:user \"{path}\"" : $"/s /u /n /i:user \"{path}\"";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0 ? null : new Exception("注册或反注册 Shell 扩展失败(regsvr32)");
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static void TerminateAndRestartExplorer()
        {
            List<Process> killedProcesses = new List<Process>();
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill();
                    killedProcesses.Add(process);
                }
            }
            foreach (Process singleKilledProcess in killedProcesses)
            {
                singleKilledProcess.WaitForExit();
            }
        }

        public static async Task<Exception> InstallShellExtension()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        return new FileNotFoundException("没有获取到当前程序的可执行文件路径");
                    }
                    if (File.Exists(shellExtensionPath))
                    {
                        await UninstallShellExtension();
                    }
                    Assembly executing = Assembly.GetExecutingAssembly();
                    if (executing.GetManifestResourceStream(embeddedResourcePath) is Stream stream)
                    {
                        using (stream)
                        {
                            byte[] shellExtBuffer = new byte[stream.Length];
                            stream.Read(shellExtBuffer, 0, shellExtBuffer.Length);
                            using (FileStream fs = File.OpenWrite(shellExtensionPath))
                            {
                                fs.Write(shellExtBuffer, 0, shellExtBuffer.Length);
                            }
                        }
                        return RegisterShellExtDll(shellExtensionPath, true) ?? await CreateAppPath() ??
                            await CreateApplication() ?? await CreateProgIdAndFileType(true);
                    }
                    else
                    {
                        return new MissingManifestResourceException("找不到内嵌的 Shell 扩展模块资源");
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        public static async Task<Exception> UninstallShellExtension()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (File.Exists(shellExtensionPath))
                    {
                        Exception exception = RegisterShellExtDll(shellExtensionPath, false);
                        if (exception != null)
                        {
                            return exception;
                        }
                        try
                        {
                            File.Delete(shellExtensionPath);
                        }
                        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                        {
                            TerminateAndRestartExplorer();
                            File.Delete(shellExtensionPath);
                        }
                        return await DeleteProgIdButNotFileType() ??
                            await DeleteApplication() ?? await DeleteAppPath();
                    }
                    else
                    {
                        return new FileNotFoundException("没有在程序所在目录找到 Shell 扩展模块");
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        private static async Task<Exception> CreateProgIdAndFileType(bool user)
        {
            RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
            return await Task.Run(() =>
            {
                try
                {
                    using (RegistryKey classes = root.OpenSubKey(regPathSoftClasses, true))
                    {
                        if (classes == null)
                        {
                            return new Exception($"无法打开注册表键：{regPathSoftClasses}");
                        }
                        if (RegNode.WriteRegNode(classes, progIdNode))
                        {
                            string[] keyNames = classes.GetSubKeyNames();
                            bool fileExtExists = false;
                            foreach (string keyName in keyNames)
                            {
                                if (basisFileSuffix.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                                {
                                    fileExtExists = true;
                                    break;
                                }
                            }
                            if (fileExtExists)
                            {
                                using (RegistryKey fileExtKey = classes.OpenSubKey(basisFileSuffix, true))
                                {
                                    if (fileExtKey != null)
                                    {
                                        if (fileExtKey.GetValue("") is string defaultVal &&
                                            !defaultVal.Equals(programId, StringComparison.OrdinalIgnoreCase))
                                        {
                                            using (RegistryKey progIdsKey = fileExtKey.CreateSubKey(regPathOpenWithProgIds, true))
                                            {
                                                progIdsKey?.SetValue(defaultVal, string.Empty);
                                            }
                                            fileExtKey.SetValue(string.Empty, programId, RegistryValueKind.String);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!RegNode.WriteRegNode(classes, basisSuffixNode))
                                {
                                    return new Exception($"写入注册表子键失败：{basisSuffixNode.Name}");
                                }
                            }
                            NativeFunctions.SHChangeNotify(
                                HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                            return null;
                        }
                        else
                        {
                            return new Exception($"写入注册表子键失败：{progIdNode.Name}");
                        }
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        private static async Task<Exception> DeleteProgIdButNotFileType()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (RegistryKey root = Registry.CurrentUser.OpenSubKey(regPathSoftClasses, true))
                    {
                        if (root != null)
                        {
                            string[] subKeyNames = root.GetSubKeyNames();
                            foreach (string keyName in subKeyNames)
                            {
                                if (keyName.Equals(programId, StringComparison.OrdinalIgnoreCase))
                                {
                                    root.DeleteSubKeyTree(keyName);
                                    NativeFunctions.SHChangeNotify(
                                        HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                                    break;
                                }
                            }
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

        private static async Task<Exception> WriteRegNodeToRegPath(string regPath, RegNode regNode)
        {
            if (string.IsNullOrEmpty(regPath))
            {
                return new Exception($"需要打开的注册表路径为空");
            }
            return await Task.Run(() =>
            {
                try
                {
                    using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(regPath, true))
                    {
                        if (parent == null)
                        {
                            return new Exception($"无法打开注册表键：{regPath}");
                        }
                        if (!RegNode.WriteRegNode(parent, regNode))
                        {
                            return new Exception($"写入注册表子键失败：{regNode.Name}");
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

        private static async Task<Exception> DeleteRegNodeFromRegPath(string regPath, RegNode regNode)
        {
            if (string.IsNullOrEmpty(regPath))
            {
                return new Exception($"需要打开的注册表路径为空");
            }
            return await Task.Run(() =>
            {
                try
                {
                    using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(regPath, true))
                    {
                        if (parent == null)
                        {
                            return new Exception($"无法打开注册表键：{regPath}");
                        }
                        parent.DeleteSubKeyTree(regNode.Name, false);
                    }
                    return null;
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        private static async Task<Exception> CreateAppPath()
        {
            return await WriteRegNodeToRegPath(regPathAppPaths, appPathsNode);
        }

        private static async Task<Exception> DeleteAppPath()
        {
            return await DeleteRegNodeFromRegPath(regPathAppPaths, appPathsNode);
        }

        private static async Task<Exception> CreateApplication()
        {
            return await WriteRegNodeToRegPath(regPathApplications, applicationNode);
        }

        private static async Task<Exception> DeleteApplication()
        {
            return await DeleteRegNodeFromRegPath(regPathApplications, applicationNode);
        }

        private const string programId = "HashCalculator.Basis";
        private const string basisFileSuffix = ".hcb";
        private const string executableName = "HashCalculator.exe";
        private const string regPathSoftClasses = "Software\\Classes";
        private static readonly string regPathApplications = $"{regPathSoftClasses}\\Applications";
        private const string regPathAppPaths = "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths";
        private const string regPathOpenWithProgIds = "OpenWithProgids";
        private static readonly RegNode appPathsNode;
        private static readonly RegNode applicationNode;
        private static readonly RegNode progIdNode;
        private static readonly RegNode basisSuffixNode;
        private static readonly string executablePath = Assembly.GetExecutingAssembly().Location;
        private static readonly string executableFolder = Path.GetDirectoryName(executablePath);
        private static readonly string shellExtensionName = Environment.Is64BitOperatingSystem ?
            "HashCalculator.dll" : "HashCalculator32.dll";
        private static readonly string embeddedResourcePath = $"HashCalculator.ShellExt.{shellExtensionName}";
        private static readonly string shellExtensionPath = Path.Combine(executableFolder, shellExtensionName);
    }
}
