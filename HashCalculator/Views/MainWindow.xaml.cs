using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommandLine;
using Handy = HandyControl;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private bool listenerAdded = false;
        private DateTime lastClipboardUpdateDateTime = DateTime.Now;
        private PresentationSource presentationSrc = null;
        private readonly MainWndViewModel viewModel = null;

        private static string[] startupArgs = null;
        private static readonly TimeSpan clipboardTriggerMinInterval =
            TimeSpan.FromMilliseconds(10);

        public static IntPtr WndHandle { get; private set; }

        public static MainWindow Current { get; private set; }

        public static int ProcessId { get; } = Process.GetCurrentProcess().Id;

        private bool ProcIdMonitorFlag { get; set; } = true;

        public MainWindow()
        {
            Current = this;
            this.viewModel = new MainWndViewModel(this);
            this.DataContext = this.viewModel;
            this.Closed += this.MainWindowClosed;
            this.Loaded += this.MainWindowLoaded;
            this.InitializeComponent();
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
            // 此处与 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Set 不重复，原因：
            // 如果是本进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
            // 1. 本进程实例 ProcessIdMonitorProc 方法内进入 if (!this.ProcIdMonitorFlag) 分支，
            // 2. 分支内再执行一次 PIdSynchronizer.Set 以保证可以有其他进程实例（如果有）能抢到锁，
            // 3. 然后在其他进程实例内启动 ComputeCrossProcessFilesMonitor 保证其他进程能监控第三方进程的参数推送。
            // 如果是其他进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
            // 则直接进入步骤 3，本进程实例 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢不到锁不会往下执行。
            Initializer.PIdSynchronizer.Set();
            foreach (DataGridColumn column in this.MainWindowDataGrid.Columns)
            {
                if (column.Header is string header)
                {
                    if (Settings.Current.ColumnsOrder.ContainsKey(header))
                    {
                        Settings.Current.ColumnsOrder[header].Width = column.Width;
                        Settings.Current.ColumnsOrder[header].Index = column.DisplayIndex;
                    }
                    else
                    {
                        Settings.Current.ColumnsOrder[header] = new ColumnProperty(
                            column.DisplayIndex, column.Width);
                    }
                }
            }
            this.GifImageLoading1?.Dispose();
            FilterAndCmdPanel.Current?.GifImageLoading2?.Dispose();
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
            this.MainWindowDataGrid.Columns.ReorderDataGridColumns(Settings.Current.ColumnsOrder);
            if (await Settings.TestCompatibilityOfShellExt() is string notification)
            {
                Handy.Controls.Growl.Error(notification, MessageToken.MainWndMsgToken);
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
                    this.viewModel.CheckHashUseClipboardText();
                }
                Settings.Current.ClipboardUpdatedByMe = false;
                this.lastClipboardUpdateDateTime = DateTime.Now;
            }
            return IntPtr.Zero;
        }

        private IEnumerable<AlgoType> GetAlgoTypesFromOption(IOptions option)
        {
            if (option != null && !string.IsNullOrEmpty(option.Algos))
            {
                List<AlgoType> resolvedAlgoTypeList = new List<AlgoType>();
                foreach (string algoTypeStr in option.Algos.Split(','))
                {
                    if (AlgosPanelModel.TryGetAlgoType(algoTypeStr, out AlgoType algoType) &&
                        algoType != AlgoType.UNKNOWN)
                    {
                        resolvedAlgoTypeList.Add(algoType);
                    }
                }
                if (resolvedAlgoTypeList.Any())
                {
                    return resolvedAlgoTypeList;
                }
            }
            return default(IEnumerable<AlgoType>);
        }

        private void InternalParseArguments(string[] args)
        {
            Parser.Default.ParseArguments<ComputeHash, VerifyHash>(args)
                .WithParsed<ComputeHash>(option =>
                {
                    if (option.FilePaths != null)
                    {
                        if (Settings.Current.ClearTableBeforeAddingFilesByCmdLine)
                        {
                            MainWndViewModel.Synchronization.Invoke(() =>
                            {
                                MainWndViewModel.Current.ClearAllTableLinesAction(null);
                            });
                        }
                        string[] filePaths = option.FilePaths.ToArray();
                        // 此处逻辑针对命令行传来的待计算文件/文件夹路径，一般由右键菜单生成命令
                        // 如果是用户手动输入命令，则这些路径有可能分属不同的父目录，所以逐个处理
                        PathPackage[] packages = new PathPackage[filePaths.Length];
                        for (int i = 0; i < filePaths.Length; ++i)
                        {
                            // 当 filePaths[i] 是分区根目录时 GetDirectoryName 返回 null
                            string parent = Path.GetDirectoryName(filePaths[i]) ?? filePaths[i];
                            PathPackage package = new PathPackage(parent, filePaths[i],
                                Settings.Current.SelectedSearchMethodForDragDrop);
                            packages[i] = package;
                            package.PresetAlgoTypes = this.GetAlgoTypesFromOption(option);
                        }
                        this.viewModel.BeginDisplayModels(packages);
                    }
                })
                .WithParsed<VerifyHash>(option =>
                {
                    if (File.Exists(option.ChecklistPath))
                    {
                        IEnumerable<AlgoType> types = this.GetAlgoTypesFromOption(option);
                        HashChecklist newChecklist = HashChecklist.File(option.ChecklistPath,
                            types);
                        if (newChecklist.ReasonForFailure != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Handy.Controls.MessageBox.Show(this, newChecklist.ReasonForFailure,
                                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                        else
                        {
                            if (Settings.Current.ClearTableBeforeAddingFilesByCmdLine)
                            {
                                MainWndViewModel.Synchronization.Invoke(() =>
                                {
                                    MainWndViewModel.Current.ClearAllTableLinesAction(null);
                                });
                            }
                            // 这里添加要计算哈希值的文件时，看作以多选文件的方式添，所以
                            // PathPackage 的 parent 参数应是 option.ChecklistPath 所在目录
                            string filesDir = Path.GetDirectoryName(option.ChecklistPath);
                            PathPackage package = new PathPackage(filesDir, filesDir, newChecklist,
                                Settings.Current.SelectedSearchMethodForChecklist);
                            package.PresetAlgoTypes = types;
                            this.viewModel.BeginDisplayModels(package);
                        }
                    }
                });
        }

        public static void PushStartupArgs(string[] args)
        {
            startupArgs = args;
        }

        /// <summary>
        /// 多进程实例模式启动使用此方法处理不同进程传入的待处理的文件、目录路径
        /// </summary>
        private void ComputeInProcessFiles(string[] args)
        {
            this.InternalParseArguments(args);
        }

        /// <summary>
        /// 单进程实例模式启动使用此方法处理不同进程传入的待处理的文件、目录路径
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

        private void DataGridHashingFilesDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is string[] data && data.Length != 0)
            {
                // 当用户把本地磁盘分区图标拖入程序窗口
                if (data[0].EndsWith(":\\"))
                {
                    // 只要确定第一个是分区根目录，那其他项也都是分区根目录
                    // 因为 Windows 不支持把不同区域的内容同时拖入程序窗口
                    this.viewModel.BeginDisplayModels(data.Select(
                        partition => new PathPackage(partition, partition,
                        Settings.Current.SelectedSearchMethodForDragDrop)).ToArray());
                }
                else
                {
                    string parentDir = Path.GetDirectoryName(data[0]);
                    this.viewModel.BeginDisplayModels(new PathPackage(parentDir, data,
                        Settings.Current.SelectedSearchMethodForDragDrop));
                }
            }
        }

        private void DataGridHashingFilesPrevKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && sender is DataGrid dataGrid)
            {
                dataGrid.SelectedItem = null;
            }
        }

        private void TextBoxHashOrFilePathPreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                !(e.Data.GetData(DataFormats.FileDrop) is string[] data) || !data.Any())
            {
                return;
            }
            this.viewModel.HashStringOrChecklistPath = data[0];
        }

        private void TextBoxHashStringOrChecklistPathPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
