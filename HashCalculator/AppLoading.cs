using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class AppLoading : Application
    {
        private static readonly string[] reqAsmbs = new string[]
        {
            "System.Buffers",
            "System.Memory",
            "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe",
            "CommandLine",
            "Crc32.NET",
            "Microsoft.Bcl.HashCode",
            "Microsoft.WindowsAPICodePack",
            "Microsoft.WindowsAPICodePack.Shell",
            "XamlAnimatedGif",
        };
        internal static readonly Assembly ExecutingAsmb = Assembly.GetExecutingAssembly();

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
            Settings.Current.RunInMultiInstMode = MappedFiler.RunMultiMode;
            Settings.SaveSettings();
        }

        private static void ApplicationStartup(object sender, StartupEventArgs e)
        {
            Settings.LoadSettings();
            Settings.ExtractEmbeddedAlgoDlls(false);
            MappedFiler.PushArgs(Settings.StartupArgs);
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            string asmbName = new AssemblyName(arg.Name).Name;
            if (reqAsmbs.Contains(asmbName))
            {
                if (ExecutingAsmb.GetManifestResourceStream(
                    string.Format("HashCalculator.Assembly.{0}.dll", asmbName)) is Stream stream)
                {
                    byte[] assemblyBytes = new byte[stream.Length];
                    stream.Read(assemblyBytes, 0, assemblyBytes.Length);
                    stream.Close();
                    return Assembly.Load(assemblyBytes);
                }
            }
            return default(Assembly);
        }

        private void RunApplication()
        {
            //this.DispatcherUnhandledException += this.UnhandledException;
            this.StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
            this.Run();
        }

        private void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string excContent;
            // 剪贴板无法打开(CLIPBRD_E_CANT_OPEN)错误代码：0x800401D0
            if ((uint)e.Exception.HResult == 0x800401D0)
            {
                excContent = "复制哈希结果失败";
            }
            else
            {
                excContent = "意外的异常，可打开帮助页面末尾链接向开发者反馈";
            }
            e.Handled = true;
            MessageBox.Show($"{excContent}：\n{e.Exception.Message}", "错误");
        }
    }
}
