using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
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

    private static string[] startupArgs = null;
    private static readonly TimeSpan clipboardTriggerMinInterval =
        TimeSpan.FromMilliseconds(10);

    public static IntPtr WndHandle { get; private set; }

    public static MainWindow Current { get; private set; }

    public static int ProcessId { get; } = Environment.ProcessId;

    private bool ProcIdMonitorFlag { get; set; } = true;

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
            this.Hide();
            this._homePageViewModel.FilterWindowInstance?.Hide();
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
        this.ProcIdMonitorFlag = false;
        this._homePage.MainDataGrid.Columns.CollectGridColumns(Settings.Current.ColumnsOrder);
        // 此处与 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Set 不重复，原因：
        // 如果是本进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
        // 1. 本进程实例 ProcessIdMonitorProc 方法内进入 if (!this.ProcIdMonitorFlag) 分支，
        // 2. 分支内再执行一次 PIdSynchronizer.Set 以保证可以有其他进程实例（如果有）能抢到锁，
        // 3. 然后在其他进程实例内启动 ComputeCrossProcessFilesMonitor 保证其他进程能监控第三方进程的参数推送。
        // 如果是其他进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
        // 则直接进入步骤 3，本进程实例 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢不到锁不会往下执行。
        Initializer.PIdSynchronizer.Set();
    }

    private async void MainWindowLoaded(object sender, RoutedEventArgs e)
    {
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
        if (startupArgs != null)
        {
            this.ComputeInProcessFiles(startupArgs);
        }
        Thread thread = new Thread(this.ProcessIdMonitorProc);
        thread.IsBackground = true;
        thread.Start();
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
                Initializer.RunMultiMode = Settings.Current.RunInMultiInstMode;
                break;
            case nameof(Settings.Current.SelectedTaskNumberLimit):
                this._homePageViewModel.Starter.BeginAdjust(Settings.Current.SelectedTaskNumberLimit);
                break;
        }
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
            PathPackage[] packages = new PathPackage[filePaths.Length];
            for (int i = 0; i < filePaths.Length; ++i)
            {
                // 当 filePaths[i] 是分区根目录时 GetDirectoryName 返回 null
                string parent = Path.GetDirectoryName(filePaths[i]) ?? filePaths[i];
                PathPackage package = new PathPackage(parent, filePaths[i], hashChecklist,
                    Settings.Current.SelectedSearchMethodForDragDrop);
                packages[i] = package;
                package.OnlyFilesThatExistInChecklist = false;
                package.PresetAlgoTypes = this.GetAlgoTypesFromOption(algoStr);
            }
            this._homePageViewModel.BeginDisplayModels(packages);
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
                Application.Current.Dispatcher.Invoke(() =>
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
                PathPackage package = new PathPackage(filesDir, filesDir, newChecklist,
                    Settings.Current.SelectedSearchMethodForChecklist);
                package.PresetAlgoTypes = types;
                this._homePageViewModel.BeginDisplayModels(package);
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

    private void InternalParseArguments(string[] args)
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

    public static void PushStartupArgs(string[] args)
    {
        startupArgs = args;
    }

    /// <summary>
    /// 多实例模式启动使用此方法处理不同进程传入的待处理的文件、目录路径
    /// </summary>
    private void ComputeInProcessFiles(string[] args)
    {
        this.InternalParseArguments(args);
    }

    /// <summary>
    /// 单实例模式启动使用此方法处理不同进程传入的待处理的文件、目录路径
    /// </summary>
    private void ComputeCrossProcessFilesMonitor()
    {
        Initializer.ExistingProcessId = ProcessId;
        while (true)
        {
            Initializer.Synchronizer.Wait();
            // ToArray 能避免 GetArgs 方法在 ParseArguments 内被执行多次
            string[] args = Initializer.GetArgs().ToArray();
            this.InternalParseArguments(args);
        }
    }

    private void ProcessIdMonitorProc()
    {
        while (true)
        {
            Initializer.PIdSynchronizer.Wait();
            if (!this.ProcIdMonitorFlag)
            {
                Initializer.PIdSynchronizer.Set();
                break;
            }
            Thread thread = new Thread(this.ComputeCrossProcessFilesMonitor);
            thread.IsBackground = true;
            thread.Start();
        }
    }

    private void EnsureWindowIsShownAndActivated()
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

    private void MenuItemShutdownApplicationClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void NotifyIconLeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
    {
        this.EnsureWindowIsShownAndActivated();
    }
}
