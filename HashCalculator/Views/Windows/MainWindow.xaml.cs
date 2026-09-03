using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using HashCalculator.IPC;
using HashCalculator.Others;
using HashCalculator.ViewModels.Pages;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpfctrls = Wpf.Ui.Controls;

namespace HashCalculator.Views.Windows;

public partial class MainWindow
{
    private bool listenerAdded = false;
    private DateTime lastClipboardUpdateDateTime = DateTime.Now;
    private PresentationSource presentationSrc = null;
    private NavigationService _navigationService = null;

    private readonly MainWindowModel _viewModel = null;
    private readonly HomePage _homePage = null;
    private readonly HomeViewModel _homePageViewModel = null;

    private static readonly TimeSpan clipboardTriggerMinInterval =
        TimeSpan.FromMilliseconds(10);

    public static IntPtr WndHandle { get; private set; }

    public static MainWindow Current { get; private set; }

    // 本实例作为"本地启动"实例（首实例或多实例模式的后续实例）启动时，
    // 由 App.StartupHandler 暂存的本机命令行参数，待主窗口 Loaded 后处理。
    // 用 static 是因为它属于进程级启动数据，且 MainWindowLoaded 可能晚于启动决策执行。
    private static string[] localStartupArguments = null;

    // 为 true 时，RunInMultiInstMode 变更不广播给其他实例。
    // 由 SetMultiModeHandler 收到跨进程广播后应用设置时置位，
    // 防止"收到广播 → 更新设置 → 触发 PropertyChanged → 又广播回去"的无限循环。
    private bool suppressAppRunMultiModeBroadcast;

    public MainWindow(
        MainWindowModel viewModel,
        HomePage homePage,
        HomeViewModel homePageViewModel,
        ISnackbarService snackbarService)
    {
        Current = this;
        this._viewModel = viewModel;
        this._homePageViewModel = homePageViewModel;
        this._homePage = homePage;
        this.DataContext = this._viewModel;
        this.InitializeComponent();
        snackbarService.SetSnackbarPresenter(this.SnackbarPresenter);
        this.InitializeNavigation();
        if (Settings.Current.SelectedApplicationThemeIndex == 0)
        {
            SystemThemeWatcher.Watch(this, Wpfctrls.WindowBackdropType.None);
        }
        // 管道监听的生命周期等于进程存活期，而窗口可能被关闭到托盘再重开，
        // 故在此启动并依赖 IPCHost 的判空保证只启动一次。
        IPCHost.Start();
    }

    private void InitializeNavigation()
    {
        INavigationViewPageProvider pageProvider = App.GetRequiredService
            <INavigationViewPageProvider>();
        this._navigationService = new NavigationService(pageProvider);
        this._navigationService.SetNavigationControl(this.NavigationView);
        this._viewModel.SetModelNavigationService(this._navigationService);
        this._homePageViewModel.SetModelNavigationService(this._navigationService);
        this.NavigationView.SelectionChanged += this.NavigationChanged;
    }

    private void NavigationChanged(Wpfctrls.NavigationView sender, RoutedEventArgs args)
    {
        if (sender.SelectedItem is Wpfctrls.INavigationViewItem item)
        {
            if (item != this._viewModel.SettingsNavigationItem
                && item.NavigationViewItemParent != this._viewModel.SettingsNavigationItem)
            {
                this._viewModel.SettingsNavigationItem.IsExpanded = false;
            }
        }
    }

    private void MainWindowClosing(object sender, CancelEventArgs e)
    {
        if (Settings.Current.HideWindowToSystemTrayWhenClosing)
        {
            this._homePageViewModel.FilterWindowInstance?.Hide();
            this.Hide();
            e.Cancel = true;
        }
        else
        {
            e.Cancel = Settings.Current.ProcessingShellExtension;
        }
    }

    private void MainWindowClosed(object sender, EventArgs e)
    {
        this.RemoveClipboardListener();
        if (this.presentationSrc is HwndSource hwndSource)
        {
            hwndSource.RemoveHook(this.WindowProcedure);
            hwndSource.Dispose();
        }
        this._homePage.MainDataGrid.Columns.CollectGridColumns(Settings.Current.ColumnsOrder);
    }

    private async void MainWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 添加主窗口的边框强调色，使其在主窗口背景色下更显眼
        //this.SetValue(BorderThicknessProperty, new Thickness(2));
        //this.SetValue(BorderBrushProperty, this.FindResource("SystemAccentColorBrush"));
        this._navigationService.Navigate(typeof(HomePage));
        WndHandle = new WindowInteropHelper(this).Handle;
        this.presentationSrc = PresentationSource.FromVisual(this);
        if (this.presentationSrc is HwndSource hwndSrc)
        {
            hwndSrc.AddHook(this.WindowProcedure);
            if (Settings.Current.MonitorNewHashStringInClipboard)
            {
                this.AddClipboardListener();
            }
        }
        Settings.Current.PropertyChanged += this.SettingsPropertyChanged;
        if (ShellExtHelper.RunningAsAdmin)
        {
            this.Title += " （管理员）";
        }
        if (localStartupArguments != null)
        {
            // 本机启动携带的参数（首实例或后续多实例），窗口 Loaded 后才处理。
            this.HandleReceivedCommandLine(localStartupArguments);
            localStartupArguments = null;
        }
        this._homePage.MainDataGrid.Columns.ReorderGridColumns(Settings.Current.ColumnsOrder);
        if (await Settings.TestCompatibilityOfShellExt() is string notification)
        {
            NotificationSender.SnackbarError(notification);
        }
        Settings.Current.PreviousVer = Info.Ver;
    }

    /// <summary>
    /// 需要立即响应的设置变更
    /// </summary>
    private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Current.MonitorNewHashStringInClipboard):
                if (Settings.Current.MonitorNewHashStringInClipboard)
                {
                    this.AddClipboardListener();
                }
                else
                {
                    this.RemoveClipboardListener();
                }
                break;
            case nameof(Settings.Current.RunInMultiInstMode):
                if (!this.suppressAppRunMultiModeBroadcast)
                {
                    this.BroadcastAppRunMultiModeChanged();
                }
                break;
            case nameof(Settings.Current.SelectedTaskNumberLimit):
                this._homePageViewModel.JobScheduler.SetConcurrency(Settings.Current.SelectedTaskNumberLimit);
                break;
        }
    }

    /// <summary>
    /// 供 SetMultiModeHandler 调用：把收到的跨进程多实例模式值应用到本实例设置。
    /// 置位抑制标志，使这次设置变更不触发向其他实例的广播，从而避免循环广播。
    /// </summary>
    internal void ApplyAppMultiModeFromIPC(bool value)
    {
        this.suppressAppRunMultiModeBroadcast = true;
        try
        {
            Settings.Current.RunInMultiInstMode = value;
        }
        finally
        {
            this.suppressAppRunMultiModeBroadcast = false;
        }
    }

    /// <summary>
    /// 本实例的多实例模式被用户改动后，把新值广播给其他所有存活实例。
    /// 广播是异步网络操作，这里不等待，用 fire-and-forget；
    /// 每个目标各自独立发送，个别失败（实例恰好在退出）不影响其余。
    /// </summary>
    private void BroadcastAppRunMultiModeChanged()
    {
        Task.Run(async () =>
        {
            byte newValue = Convert.ToByte(Settings.Current.RunInMultiInstMode);
            foreach (InstanceEndpoint endpoint in InstanceDiscovery.Discover())
            {
                await CommandClient.SendAsync(endpoint.PipeName, IPCMessageKind.SetAppMultiMode,
                    new byte[] { newValue });
            }
        });
    }

    public void AddClipboardListener()
    {
        if (WndHandle != IntPtr.Zero && !this.listenerAdded)
        {
            this.listenerAdded = USER32.AddClipboardFormatListener(WndHandle);
        }
    }

    public void RemoveClipboardListener()
    {
        if (this.listenerAdded && WndHandle != IntPtr.Zero)
        {
            USER32.RemoveClipboardFormatListener(WndHandle);
            this.listenerAdded = false;
        }
    }

    private IntPtr WindowProcedure(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool _)
    {
        if (msg == WM.WM_CLIPBOARDUPDATE)
        {
            if (!Settings.Current.ClipboardUpdatedByMe &&
                DateTime.Now - this.lastClipboardUpdateDateTime > clipboardTriggerMinInterval)
            {
                this._homePageViewModel.CheckHashUseClipboardText();
            }
            Settings.Current.ClipboardUpdatedByMe = false;
            this.lastClipboardUpdateDateTime = DateTime.Now;
        }
        return IntPtr.Zero;
    }

    private List<AlgoType> GetAlgoTypesFromOption(string algoStr)
    {
        if (!string.IsNullOrEmpty(algoStr))
        {
            List<AlgoType> resolvedAlgoTypeList = new List<AlgoType>();
            foreach (string algoTypeStr in algoStr.Split(','))
            {
                if (AlgorithmsModel.TryGetAlgoType(algoTypeStr, out AlgoType algoType) &&
                    algoType != AlgoType.UNKNOWN)
                {
                    resolvedAlgoTypeList.Add(algoType);
                }
            }
            if (resolvedAlgoTypeList.Count != 0)
            {
                return resolvedAlgoTypeList;
            }
        }
        return default(List<AlgoType>);
    }

    private void ParsedComputeHashHandler(IEnumerable<string> paths, string algoStr)
    {
        if (paths != null)
        {
            HashChecklist hashChecklist = null;
            if (Settings.Current.ClearTableBeforeAddingFilesByCmdLine)
            {
                Synchronization.UI.Invoke(() =>
                {
                    this._homePageViewModel.ClearAllTableLinesAction(null);
                });
            }
            if (Settings.Current.UseExistingClipboardTextForCheck)
            {
                hashChecklist = this._homePageViewModel.TestClipboardTextGetChecklist();
            }
            string[] filePaths = paths.Where(i => File.Exists(i) || Directory.Exists(i)).ToArray();
            // 此处逻辑针对命令行传来的待计算文件/文件夹路径，一般由右键菜单生成命令
            // 如果是用户手动输入命令，则这些路径有可能分属不同的父目录，所以逐个处理
            PathPackage[] pathPackages = new PathPackage[filePaths.Length];
            for (int i = 0; i < filePaths.Length; ++i)
            {
                // 当 filePaths[i] 是分区根目录时 GetDirectoryName 返回 null
                string parent = Path.GetDirectoryName(filePaths[i]) ?? filePaths[i];
                PathPackage package = new PathPackage(parent, filePaths[i], hashChecklist,
                    Settings.Current.SelectedSearchMethodForDragDrop);
                pathPackages[i] = package;
                package.OnlyFilesThatExistInChecklist = false;
                package.PresetAlgoTypes = this.GetAlgoTypesFromOption(algoStr);
            }
            this._homePageViewModel.BeginDisplayModels(pathPackages);
        }
    }

    private void ParsedVerifyHashHandler(string checklistPath, string algoStr)
    {
        if (File.Exists(checklistPath))
        {
            List<AlgoType> types = this.GetAlgoTypesFromOption(algoStr);
            HashChecklist newChecklist = HashChecklist.File(checklistPath,
                types);
            if (newChecklist.ReasonForFailure != null)
            {
                Synchronization.UI.Invoke(() =>
                {
                    NotificationSender.ShowMessageBox(this, "错误", newChecklist.ReasonForFailure);
                });
            }
            else
            {
                if (Settings.Current.ClearTableBeforeAddingFilesByCmdLine)
                {
                    Synchronization.UI.Invoke(() =>
                    {
                        this._homePageViewModel.ClearAllTableLinesAction(null);
                    });
                }
                // 这里添加要计算哈希值的文件时，看作以多选文件的方式添，所以
                // PathPackage 的 parent 参数应是 checklistPath 所在目录
                string filesDir = Path.GetDirectoryName(checklistPath);
                PathPackage pathPackage = new PathPackage(filesDir, filesDir, newChecklist,
                    Settings.Current.SelectedSearchMethodForChecklist);
                pathPackage.PresetAlgoTypes = types;
                this._homePageViewModel.BeginDisplayModels(pathPackage);
            }
        }
    }

    private List<string> RewriteArgsWithVerb(string[] args)
    {
        List<string> argList = args.ToList();
        if (Settings.Current.SelectionWhenNoVerbIsSpecified == MenuType.CheckHash)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (File.Exists(args[i]))
                {
                    argList.Insert(i, CmdOptions.ChecklistArgLong);
                    break;
                }
            }
            argList.Insert(0, CmdOptions.CheckHashVerb);
        }
        else
        {
            argList.Insert(0, CmdOptions.ComputeHashVerb);
        }
        return argList;
    }

    /// <summary>
    /// 由 App.StartupHandler 在判定本实例为「本地启动」（首实例或多实例模式的后续实例）
    /// 时调用，暂存本机携带的命令行参数，待主窗口 Loaded 后再由 HandleReceivedCommandLine 处理。
    /// 用 static 是因为 MainWindow 构造与 Loaded 都晚于启动决策执行。
    /// </summary>
    internal static void AcceptLocalStartupArguments(string[] args)
    {
        localStartupArguments = args;
    }

    /// <summary>
    /// 处理一条待执行的文件计算/校验命令行参数。
    /// 是本机启动携带的参数与跨进程 ParseArgumentsHandler 转发的参数的共同入口，
    /// 因此为 internal 供 handler 通过 MainWindow.Current 调用。
    /// 依赖 <see cref="Settings.Current"/> 与主窗口所属的 HomeViewModel，须在 UI 线程执行。
    /// </summary>
    internal void HandleReceivedCommandLine(string[] args)
    {
        ParseResult result = CmdOptions.RootCommand.Parse(args);
        // 仅当"未指定任何 verb 且存在参数 token"时尝试自动插入 verb，其余情况忽略
        if (result.Errors.Count != 0 &&
            !result.Tokens.Any(t => t.Type == TokenType.Command) &&
            result.Tokens.Any(t => t.Type == TokenType.Argument))
        {
            result = CmdOptions.RootCommand.Parse(this.RewriteArgsWithVerb(args));
        }
        if (result.Errors.Count != 0)
        {
            return;
        }
        if (result.GetResult(CmdOptions.ComputeCommand) is CommandResult computeCmd)
        {
            string algoStr = computeCmd.GetValue(CmdOptions.AlgoOption);
            IEnumerable<string> paths = computeCmd.GetValue(CmdOptions.PathsToCompute);
            this.ParsedComputeHashHandler(paths, algoStr);
        }
        else if (result.GetResult(CmdOptions.CheckHashCommand) is CommandResult verifyCmd)
        {
            string algoStr = verifyCmd.GetValue(CmdOptions.AlgoOption);
            string listPath = verifyCmd.GetValue(CmdOptions.CheckListOption);
            this.ParsedVerifyHashHandler(listPath, algoStr);
        }
    }

    internal void EnsureWindowIsShownAndActivated()
    {
        if (!this.IsVisible)
        {
            this.Show();
        }
        if (this._homePageViewModel.FilterWindowInstance?.IsVisible == false)
        {
            this._homePageViewModel.FilterWindowInstance.Show();
        }
        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = Settings.Current.MainWindowStateWithoutMinimized;
        }
        if (!this.IsActive)
        {
            this.Activate();
        }
    }

    /// <summary>把主窗口导航到指定页面，供本进程各处的统一调用</summary>
    internal void NavigateTo(Type pageType)
    {
        this._navigationService.Navigate(pageType);
    }

    private void MenuItemNavigateToHomePageClick(object sender, RoutedEventArgs e)
    {
        this.EnsureWindowIsShownAndActivated();
        this._navigationService.Navigate(typeof(HomePage));
    }

    private void MenuItemNavigateToAlgosPanelClick(object sender, RoutedEventArgs e)
    {
        this.EnsureWindowIsShownAndActivated();
        this._navigationService.Navigate(typeof(AlgosPanelPage));
    }

    private void MenuItemNavigateToSettingsClick(object sender, RoutedEventArgs e)
    {
        this.EnsureWindowIsShownAndActivated();
        this._navigationService.Navigate(typeof(SettingsPanelPage));
    }

    private void MenuItemHideOrShowMainWindowClick(object sender, RoutedEventArgs e)
    {
        if (this._homePageViewModel.FilterWindowInstance?.IsVisible == true)
        {
            this._homePageViewModel.FilterWindowInstance.Hide();
        }
        if (this.IsVisible)
        {
            this.Hide();
        }
    }

    private void MenuItemShutdownApplicationClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void NotifyIconLeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
    {
        this.EnsureWindowIsShownAndActivated();
    }
}
