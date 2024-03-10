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

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private bool hwndSourceHookAdded = false;
        private bool listenerAdded = false;
        private long elapsedTickcountsSinceLastUpdateOfClipboard = 0;
        private readonly MainWndViewModel viewModel = new MainWndViewModel();
        private static string[] startupArgs = null;

        public static MainWindow This { get; private set; }

        public static IntPtr WndHandle { get; private set; }

        public static int ProcessId { get; } = Process.GetCurrentProcess().Id;

        private bool ProcIdMonitorFlag { get; set; } = true;

        public MainWindow()
        {
            This = this;
            this.viewModel.OwnerWnd = this;
            this.DataContext = this.viewModel;
            this.Closed += this.MainWindowClosed;
            this.Loaded += this.MainWindowLoaded;
            this.ContentRendered += this.MainWindowRendered;
            this.InitializeComponent();
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            this.RemoveClipboardListener();
            this.ProcIdMonitorFlag = false;
            // 此处与 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Set 不重复，原因：
            // 如果是本进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
            // 1. 本进程实例 ProcessIdMonitorProc 方法内进入 if (!this.ProcIdMonitorFlag) 分支，
            // 2. 分支内再执行一次 PIdSynchronizer.Set 以保证可以有其他进程实例（如果有）能抢到锁，
            // 3. 然后在其他进程实例内启动 ComputeCrossProcessFilesMonitor 保证其他进程能监控第三方进程的参数推送。
            // 如果是其他进程实例内的 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢到了锁，
            // 则直接进入步骤 3，本进程实例 ProcessIdMonitorProc 方法内的 PIdSynchronizer.Wait 抢不到锁不会往下执行。
            Initializer.PIdSynchronizer.Set();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            WndHandle = new WindowInteropHelper(this).Handle;
            if (startupArgs != null)
            {
                this.ComputeInProcessFiles(startupArgs);
            }
            Thread thread = new Thread(this.ProcessIdMonitorProc);
            thread.IsBackground = true;
            thread.Start();
        }

        private void MainWindowRendered(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSrc)
            {
                hwndSrc.AddHook(new HwndSourceHook(this.WndProc));
                this.hwndSourceHookAdded = true;
                if (Settings.Current.MonitorNewHashStringInClipboard)
                {
                    this.AddClipboardListener();
                }
            }
            Settings.Current.PropertyChanged += this.SettingsPropertyChanged;
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
            if (this.hwndSourceHookAdded && WndHandle != IntPtr.Zero &&
                !this.listenerAdded)
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

        private IntPtr WndProc(IntPtr h, int msg, IntPtr w, IntPtr l, ref bool _)
        {
            if (msg == WM.WM_CLIPBOARDUPDATE)
            {
                if (!Settings.Current.ClipboardUpdatedByMe &&
                    DateTime.Now.Ticks - this.elapsedTickcountsSinceLastUpdateOfClipboard > 600)
                {
                    this.viewModel.SetHashStringOrChecklistPath();
                }
                Settings.Current.ClipboardUpdatedByMe = false;
                this.elapsedTickcountsSinceLastUpdateOfClipboard = DateTime.Now.Ticks;
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
                    if (Enum.TryParse(algoTypeStr, true, out AlgoType algoType))
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
                        PathPackage package = new PathPackage(
                            option.FilePaths, Settings.Current.SelectedSearchMethodForDragDrop);
                        package.PresetAlgoTypes = this.GetAlgoTypesFromOption(option);
                        this.viewModel.BeginDisplayModels(package);
                    }
                })
                .WithParsed<VerifyHash>(option =>
                {
                    if (File.Exists(option.ChecklistPath))
                    {
                        HashChecklist newChecklist = new HashChecklist(option.ChecklistPath);
                        if (newChecklist.ReasonForFailure != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, newChecklist.ReasonForFailure, "错误",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                        else
                        {
                            PathPackage package = new PathPackage(Path.GetDirectoryName(option.ChecklistPath),
                                Settings.Current.SelectedSearchMethodForChecklist, newChecklist);
                            package.PresetAlgoTypes = this.GetAlgoTypesFromOption(option);
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
                this.viewModel.BeginDisplayModels(
                    new PathPackage(data, Settings.Current.SelectedSearchMethodForDragDrop));
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
