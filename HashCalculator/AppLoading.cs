﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class AppLoading : Application
    {
        private static readonly string[] assemblyNames = new string[]
        {
            "System.Buffers",
            "System.Memory",
            "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe",
            "CommandLine",
            "Microsoft.Bcl.HashCode",
            "Microsoft.WindowsAPICodePack",
            "Microsoft.WindowsAPICodePack.Shell",
            "Newtonsoft.Json",
            "XamlAnimatedGif",
        };
        internal static readonly Assembly Executing = Assembly.GetExecutingAssembly();

        [STAThread()]
        public static void Main(string[] args)
        {
            Settings.StartupArgs = args;
            Settings.SetProcessEnvVar();
            AppLoading app = new AppLoading();
            app.Exit += ApplicationExit;
            app.Startup += ApplicationStartup;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            app.RunApplication();
        }

        private static void ApplicationExit(object sender, ExitEventArgs e)
        {
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            Settings.SaveSettings();
        }

        private static void ApplicationStartup(object sender, StartupEventArgs e)
        {
            Settings.LoadSettings();
            Settings.ExtractEmbeddedAlgoDllFile(false);
            Initializer.PushArgs(Settings.StartupArgs);
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            try
            {
                string assemblyName = new AssemblyName(arg.Name).Name;
                if (assemblyNames.Contains(assemblyName))
                {
                    using (Stream stream = Executing.GetManifestResourceStream(
                        string.Format("HashCalculator.Assembly.{0}.dll", assemblyName)))
                    {
                        byte[] assemblyBytes = new byte[stream.Length];
                        if (stream?.Read(assemblyBytes, 0, assemblyBytes.Length) == assemblyBytes.Length)
                        {
                            return Assembly.Load(assemblyBytes);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private void RunApplication()
        {
            //this.DispatcherUnhandledException += this.UnhandledException;
            this.StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
            this.Run();
        }

        private void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 剪贴板无法打开(CLIPBRD_E_CANT_OPEN)错误代码：0x800401D0
            string excContent = (uint)e.Exception.HResult == 0x800401D0 ?
                "复制哈希结果失败" : "未知异常，可打开关于页面打开反馈链接向开发者反馈";
            e.Handled = true;
            MessageBox.Show($"{excContent}：\n{e.Exception.Message}", "错误");
        }
    }
}
