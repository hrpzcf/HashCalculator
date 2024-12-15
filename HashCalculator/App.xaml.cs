using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class App : Application
    {
        internal static readonly Assembly Executing = Assembly.GetExecutingAssembly();

        private void StartupHandler(object sender, StartupEventArgs e)
        {
            Settings.SetProcessEnvVar();
            Settings.LoadSettings();
            Initializer.ParseArgsForShell(e.Args);
            Settings.ExtractEmbeddedAlgoDll(false);
            Initializer.PushArgs(e.Args);
        }

        private void ExitHandler(object sender, ExitEventArgs e)
        {
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            Settings.SaveSettings();
        }

        private void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"遇到异常即将退出：\n{e.Exception.Message}\n\n" +
                $"问题反馈：软件设置 - 关于软件 - 问题反馈。", "错误");
            Environment.Exit(3);
        }
    }
}
