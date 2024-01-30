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
        private static readonly string[] reqAsmbNames = new string[]
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
            Settings.ExtractEmbeddedAlgoDllAndReadme(false);
            Initializer.PushArgs(Settings.StartupArgs);
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            try
            {
                string asmbName = new AssemblyName(arg.Name).Name;
                if (!reqAsmbNames.Contains(asmbName))
                {
                    return default(Assembly);
                }
                using (Stream stream = Executing.GetManifestResourceStream(
                    string.Format("HashCalculator.Assembly.{0}.dll", asmbName)))
                {
                    if (stream?.TryGetBytes(out byte[] assemblyBytes) == true)
                    {
                        return Assembly.Load(assemblyBytes);
                    }
                }
            }
            catch (Exception)
            {
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
