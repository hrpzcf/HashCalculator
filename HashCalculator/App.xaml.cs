using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class App : Application
    {
        private bool _isSessionEndingHandled = false;

        private void StartupHandler(object sender, StartupEventArgs e)
        {
            // 用于兼容 .NET Core 及以上版本，避免找不到 GB18030 等编码。
            // 注册 CodePagesEncodingProvider.Instance 后，
            // 在 Windows 上， GetEncoding(0) 返回与系统的活动代码页匹配的编码，
            // 该代码与 .NET Framework 中的行为相同。
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Settings.LoadSettings();
            Initializer.ParseArgsForShell(e.Args);
            Initializer.PushArgs(e.Args);
        }

        private void ApplicationFinalization()
        {
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            Settings.SaveSettings();
        }

        private void ExitHandler(object sender, ExitEventArgs e)
        {
            if (!this._isSessionEndingHandled)
            {
                this.ApplicationFinalization();
            }
        }

        private void ApplicationSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            this.ApplicationFinalization();
            this._isSessionEndingHandled = true;
        }

        private void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"程序异常即将退出：{e.Exception.Message}\n如何反馈问题：软件设置 - 关于软件 - 问题反馈。", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(3);
        }
    }
}
