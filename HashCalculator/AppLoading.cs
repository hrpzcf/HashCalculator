using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class AppLoading : Application
    {
        [STAThread()]
        public static void Main(string[] args)
        {
            Settings.StartupArgs = args;
            AppLoading app = new AppLoading();
            app.Exit += ApplicationExit;
            app.Startup += ApplicationStartup;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            app.RunApplication();
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            string asmbName = new AssemblyName(arg.Name).Name;
            if (!(asmbName == "XamlAnimatedGif" ||
                asmbName == "CommandLine" ||
                asmbName == "BouncyCastle.Cryptography" ||
                asmbName == "Microsoft.WindowsAPICodePack" ||
                asmbName == "Microsoft.WindowsAPICodePack.Shell"))
            {
                return default;
            }
            Assembly executingAsmb = Assembly.GetExecutingAssembly();
            string resName = "HashCalculator.Assembly." + asmbName + ".dll";
            if (!(executingAsmb.GetManifestResourceStream(resName) is Stream stream))
            {
                return default;
            }
            byte[] assemblyData = new byte[stream.Length];
            stream.Read(assemblyData, 0, assemblyData.Length);
            stream.Close();
            return Assembly.Load(assemblyData);
        }

        private static void ApplicationExit(object sender, ExitEventArgs e)
        {
            Settings.Current.RunInMultiInstMode = MappedFiler.RunMultiMode;
            Settings.SaveSettings();
        }

        private static void ApplicationStartup(object sender, StartupEventArgs e)
        {
            Settings.LoadSettings();
            MappedFiler.PushArgs(Settings.StartupArgs);
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
