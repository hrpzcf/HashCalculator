using System.Text;
using System.Windows;
using System.Windows.Threading;
using HashCalculator.Services;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HashCalculator;

public partial class App : Application
{
    private bool _exceptionWindowShowed = false;
    private bool _isSessionEndingHandled = false;
    private ExceptionWindow _exceptionMessageBox = null;

    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            _ = services.AddSingleton<MainWindow>();
            _ = services.AddSingleton<MainWndViewModel>();
            _ = services.AddSingleton<HomePage>();
            _ = services.AddSingleton<HomeViewModel>();
            // 应用生命周期
            _ = services.AddHostedService<ApplicationHostService>();
        }).Build();

    public static T GetRequiredService<T>() where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

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
        _host.Start();
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
        _host.StopAsync().Wait();
        _host.Dispose();
    }

    private void ApplicationSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        this.ApplicationFinalization();
        this._isSessionEndingHandled = true;
    }

    private void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        this._exceptionMessageBox ??= new ExceptionWindow()
        {
            Owner = HashCalculator.MainWindow.Current
        };
        this._exceptionMessageBox.Model.AddMessage(e.Exception.Message, e.Exception.StackTrace);
        if (!this._exceptionWindowShowed)
        {
            this._exceptionWindowShowed = true;
            this._exceptionMessageBox.ShowDialog();
        }
    }
}
