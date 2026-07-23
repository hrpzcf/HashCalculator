using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CommandLine;
using HashCalculator.Others;
using HashCalculator.ViewModels.Pages;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpfctrls = Wpf.Ui.Controls;

namespace HashCalculator.Views.Windows
{
    public partial class MainWindow
    {
        private bool listenerAdded = false;
        private DateTime lastClipboardUpdateDateTime = DateTime.Now;
        private PresentationSource presentationSrc = null;

        private readonly HomePage _homePage = null;
        private readonly MainWindowModel _viewModel = null;

        private static string[] startupArgs = null;
        private static readonly TimeSpan clipboardTriggerMinInterval =
            TimeSpan.FromMilliseconds(10);

        public static IntPtr WndHandle { get; private set; }

        public static MainWindow Current { get; private set; }

        public static int ProcessId { get; } = Environment.ProcessId;

        private bool ProcIdMonitorFlag { get; set; } = true;

        public MainWindow(
            MainWindowModel viewModel,
            ISnackbarService snackbarService,
            INavigationService navigationService,
            HomePage homePage)
        {
            Current = this;
            this._homePage = homePage;
            this._viewModel = viewModel;
            this.DataContext = this._viewModel;
            this.InitializeComponent();
            snackbarService.SetSnackbarPresenter(this.SnackbarPresenter);
            navigationService.SetNavigationControl(this.NavigationView);
            this.NavigationView.SelectionChanged += this.SelectionChanged;
        }

        private void SelectionChanged(Wpfctrls.NavigationView sender, RoutedEventArgs args)
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
            e.Cancel = Settings.Current.ProcessingShellExtension;
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
            // 深色主题下把窗口背景调浅(WPF UI 默认 #FF202020 太黑)。
            // Changed 事件在 Apply 内部、RestoreContentBackground 之前触发,
            // 所以在这里覆盖 ApplicationBackgroundBrush 能被 RestoreContentBackground 取到。
            this.UpdateWindowBackgroundByTheme(default, default);
            ApplicationThemeManager.Changed += this.UpdateWindowBackgroundByTheme;
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

        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Current.MonitorNewHashStringInClipboard))
            {
                if (Settings.Current.MonitorNewHashStringInClipboard)
                {
                    this.AddClipboardListener();
                }
                else
                {
                    this.RemoveClipboardListener();
                }
            }
        }

        /// <summary>
        /// 深色主题下将窗口背景色调浅(WPF UI 默认 #FF202020 太黑)。
        /// 通过覆盖窗口自身资源字典里的 ApplicationBackgroundBrush 实现:
        /// RestoreContentBackground 取的是 window.Resources["ApplicationBackgroundBrush"],
        /// 所以在这里覆盖能被它正确取到。
        /// </summary>
        private void UpdateWindowBackgroundByTheme(ApplicationTheme t, Color c)
        {
            // 不用 t 判断是因为要在订阅前手动运行此方法，此时 t 是手动传入的 default。
            if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
            {
                this.Resources["ApplicationBackgroundBrush"] = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#252525"));
            }
            else
            {
                // Remove 后从窗口资源取到 null，触发 fallback(GetFallbackBackgroundBrush),
                // fallback 会根据当前主题返回对应的原值(深色 #FF202020，浅色 #FFFAFAFA)
                this.Resources.Remove("ApplicationBackgroundBrush");
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
                    HomeViewModel.Current.CheckHashUseClipboardText();
                }
                Settings.Current.ClipboardUpdatedByMe = false;
                this.lastClipboardUpdateDateTime = DateTime.Now;
            }
            return IntPtr.Zero;
        }

        private List<AlgoType> GetAlgoTypesFromOption(IOptions option)
        {
            if (option != null && !string.IsNullOrEmpty(option.Algos))
            {
                List<AlgoType> resolvedAlgoTypeList = new List<AlgoType>();
                foreach (string algoTypeStr in option.Algos.Split(','))
                {
                    if (AlgosPanelViewModel.TryGetAlgoType(algoTypeStr, out AlgoType algoType) &&
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

        private void ParsedComputeHashHandler(ComputeHash option)
        {
            if (option.FilePaths != null)
            {
                HashChecklist hashChecklist = null;
                if (Settings.Current.ClearTableBeforeAddingFilesByCmdLine)
                {
                    Synchronization.UI.Invoke(() =>
                    {
                        HomeViewModel.Current.ClearAllTableLinesAction(null);
                    });
                }
                if (Settings.Current.UseExistingClipboardTextForCheck)
                {
                    hashChecklist = HomeViewModel.Current.TestClipboardTextGetChecklist();
                }
                string[] filePaths = option.FilePaths.Where(i => File.Exists(i) || Directory.Exists(i)).ToArray();
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
                    package.PresetAlgoTypes = this.GetAlgoTypesFromOption(option);
                }
                HomeViewModel.Current.BeginDisplayModels(packages);
            }
        }

        private void ParsedVerifyHashHandler(VerifyHash option)
        {
            if (File.Exists(option.ChecklistPath))
            {
                List<AlgoType> types = this.GetAlgoTypesFromOption(option);
                HashChecklist newChecklist = HashChecklist.File(option.ChecklistPath,
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
                            HomeViewModel.Current.ClearAllTableLinesAction(null);
                        });
                    }
                    // 这里添加要计算哈希值的文件时，看作以多选文件的方式添，所以
                    // PathPackage 的 parent 参数应是 option.ChecklistPath 所在目录
                    string filesDir = Path.GetDirectoryName(option.ChecklistPath);
                    PathPackage package = new PathPackage(filesDir, filesDir, newChecklist,
                        Settings.Current.SelectedSearchMethodForChecklist);
                    package.PresetAlgoTypes = types;
                    HomeViewModel.Current.BeginDisplayModels(package);
                }
            }
        }

        private void NotParsedArgumentsHandler(byte degree, IEnumerable<Error> errors, string[] args)
        {
            using (IEnumerator<Error> enumerator = errors.GetEnumerator())
            {
                // 判断集合元素数量为空或者 1 个以上元素直接返回
                if (!enumerator.MoveNext())
                {
                    return;
                }
                Error error = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    return;
                }
                // 有命令但没有指定谓词出现 BadVerbSelectedError
                if (error?.Tag != ErrorType.BadVerbSelectedError)
                {
                    return;
                }
            }
            bool commandGood = false;
            List<string> argList = args.ToList();
            if (Settings.Current.SelectionWhenNoVerbIsSpecified == MenuType.CheckHash)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    if (File.Exists(args[i]))
                    {
                        argList.Insert(i, VerifyHash.Checklist);
                        commandGood = true;
                        break;
                    }
                }
                argList.Insert(0, VerifyHash.Verb);
            }
            else
            {
                argList.Insert(0, ComputeHash.Verb);
                commandGood = true;
            }
            if (commandGood)
            {
                this.InternalParseArguments(argList.ToArray(), ++degree);
            }
        }

        private void InternalParseArguments(string[] args, byte degree = 1)
        {
            ParserResult<object> result = Parser.Default.ParseArguments<ComputeHash, VerifyHash>(args);
            if (result.Value is ComputeHash computeHashOption)
            {
                this.ParsedComputeHashHandler(computeHashOption);
            }
            else if (result.Value is VerifyHash verifyHashOption)
            {
                this.ParsedVerifyHashHandler(verifyHashOption);
            }
            else if (result.Value is null && degree < 2)
            {
                result.WithNotParsed(errorList => this.NotParsedArgumentsHandler(degree, errorList, args));
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
    }
}
