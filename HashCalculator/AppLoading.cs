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
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppLoading app = new AppLoading();
            app.Exit += ApplicationExit;
            app.Startup += ApplicationStartup;
            app.RunApplication();
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            string asmbName = new AssemblyName(arg.Name).Name;
            if (!(asmbName == "BouncyCastle.Cryptography" ||
                asmbName == "Microsoft.WindowsAPICodePack" ||
                asmbName == "Microsoft.WindowsAPICodePack.Shell"))
                return default;
            Assembly asmb = Assembly.GetExecutingAssembly();
            string resName = "HashCalculator.Assembly." + asmbName + ".dll";
            if (!(asmb.GetManifestResourceStream(resName) is Stream stream))
                return default;
            byte[] assemblyData = new byte[stream.Length];
            stream.Read(assemblyData, 0, assemblyData.Length);
            stream.Close();
            return Assembly.Load(assemblyData);
        }

        private static void ApplicationExit(object sender, ExitEventArgs e)
        {

        }

        private static void ApplicationStartup(object sender, StartupEventArgs e)
        {

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
                excContent = "复制哈希值失败";
            else
                excContent = "意外的异常，可打开帮助页面末尾链接向开发者反馈";
            e.Handled = true;
            MessageBox.Show($"{excContent}：\n{e.Exception.Message}", "错误");
        }
    }
}
