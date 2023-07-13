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
                return process.ExitCode == 0 ? null : new Exception("注册或反注册右键菜单扩展失败(regsvr32)");
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

        public static async Task<Exception> SetContextMenuAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        return new FileNotFoundException("没有获取到当前程序的可执行文件路径");
                    }
                    if (File.Exists(shellExtensionPath))
                    {
                        try
                        {
                            File.Delete(shellExtensionPath);
                        }
                        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                        {
                            TerminateAndRestartExplorer();
                        }
                    }
                    string uri = "HashCalculator.ShellExt." + shellExtensionName;
                    Assembly executing = Assembly.GetExecutingAssembly();
                    if (executing.GetManifestResourceStream(uri) is Stream stream)
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
                        return RegisterShellExtDll(shellExtensionPath, true);
                    }
                    else
                    {
                        return new MissingManifestResourceException("找不到内嵌的右键菜单扩展模块");
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        public static async Task<Exception> DelContextMenuAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(shellExtensionPath))
                    {
                        Exception exception = RegisterShellExtDll(shellExtensionPath, false);
                        if (exception == null)
                        {
                            try
                            {
                                File.Delete(shellExtensionPath);
                            }
                            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                            {
                                TerminateAndRestartExplorer();
                                File.Delete(shellExtensionPath);
                            }
                            return null;
                        }
                        return exception;
                    }
                    else
                    {
                        return new FileNotFoundException("没有在程序所在目录找到右键菜单扩展模块");
                    }
                }
                catch (Exception exception)
                {
                    return exception;
                }
            });
        }

        private static readonly string executablePath = Assembly.GetExecutingAssembly().Location;
        private static readonly string shellExtensionFolder = Path.GetDirectoryName(executablePath);
        private static readonly string shellExtensionName = Environment.Is64BitOperatingSystem ?
            "HashCalculator.dll" : "HashCalculator32.dll";
        private static readonly string shellExtensionPath = Path.Combine(shellExtensionFolder, shellExtensionName);
    }
}
