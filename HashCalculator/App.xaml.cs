using System.Text;
using System.Windows;
using System.Windows.Threading;
using HashCalculator.Services;
using HashCalculator.ViewModels.Pages;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Pages;
using HashCalculator.Views.UserControls;
using HashCalculator.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.DependencyInjection;

namespace HashCalculator;

public partial class App : Application
{
    private bool _exceptionWindowShowed = false;
    private bool _isSessionEndingHandled = false;
    private ExceptionWindow _exceptionMessageBox = null;

    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<INavigationViewPageProvider,
                DependencyInjectionNavigationViewPageProvider>();

            services.AddHostedService<ApplicationHostService>();

            services.AddSingleton<ISnackbarService, SnackbarService>();

            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<HomePage>();

            services.AddSingleton<MainWindowModel>();
            services.AddSingleton<MainWindow>();

            services.AddSingleton<SettingsPanelPage>();
            services.AddSingleton<GeneralSettingsPage>();
            services.AddSingleton<InterfaceSettingsPage>();
            services.AddSingleton<TaskSettingsPage>();
            services.AddSingleton<MenuSettingsPage>();
            services.AddSingleton<AliasSettingsPage>();
            services.AddSingleton<CopySettingsPage>();
            services.AddSingleton<ExportSettingsPage>();
            services.AddSingleton<ParsingSchemeSettingsPage>();
            services.AddSingleton<ShortcutSettingsPage>();
            services.AddSingleton<ConfigSettingsPage>();
            services.AddSingleton<AboutSettingsPage>();

            services.AddSingleton<AlgosPanelPage>();
            services.AddSingleton<AlgorithmsModel>();

            services.AddSingleton<DataGridFiltersControl>();
            services.AddSingleton<DataGridOperationsControl>();
            services.AddSingleton<FilterOperationModel>();
            // 因为 FilterOperationWindow 可能会经常关闭打开，
            // 所以不能注册为 Singleton 模式。
            services.AddTransient<FilterOperationWindow>();
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
        // 注册主题覆盖字典的自动替换（监听 ApplicationThemeManager.Changed）。
        ThemeOverridesManager.Initialize();
        // 必须要在 Settings.LoadSettings 后执行，否则它们依赖的 Settings 未就绪。
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
            Owner = Views.Windows.MainWindow.Current
        };
        this._exceptionMessageBox.Model.AddMessage(e.Exception.Message, e.Exception.StackTrace);
        if (!this._exceptionWindowShowed)
        {
            this._exceptionWindowShowed = true;
            this._exceptionMessageBox.ShowDialog();
        }
    }
}
