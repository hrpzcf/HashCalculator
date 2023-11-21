using System;
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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private bool hwndSourceHookAdded = false;
        private bool listenerAdded = false;
        private int tickCountWhenTextOrBasisPathWasLastSet = 0;
        private readonly MainWndViewModel viewModel = new MainWndViewModel();
        private static readonly int maxAlgoEnumInt = Enum.GetNames(typeof(AlgoType)).Length - 1;
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
            MappedFiler.PIdSynchronizer.Set();
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
                this.listenerAdded = NativeFunctions.AddClipboardFormatListener(WndHandle);
            }
        }

        public void RemoveClipboardListener()
        {
            if (this.listenerAdded && WndHandle != IntPtr.Zero)
            {
                NativeFunctions.RemoveClipboardFormatListener(WndHandle);
                this.listenerAdded = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
        {
            switch (msg)
            {
                case WM.WM_CLIPBOARDUPDATE:
                    if (!Settings.Current.ClipboardUpdatedByMe &&
                        Environment.TickCount - this.tickCountWhenTextOrBasisPathWasLastSet > 600)
                    {
                        this.viewModel.SetTextOnHashStringOrBasisPath();
                    }
                    Settings.Current.ClipboardUpdatedByMe = false;
                    this.tickCountWhenTextOrBasisPathWasLastSet = Environment.TickCount;
                    break;
            }
            return IntPtr.Zero;
        }

        private void InternalParseArguments(string[] args)
        {
            Parser.Default.ParseArguments<ComputeHash, VerifyHash>(args)
                .WithParsed<ComputeHash>(option =>
                {
                    if (option.FilePaths != null)
                    {
                        PathPackage package = new PathPackage(
                            option.FilePaths, Settings.Current.SelectedSearchPolicy);
                        if (!string.IsNullOrEmpty(option.Algo))
                        {
                            if (int.TryParse(option.Algo, out int algo))
                            {
                                if (algo > 0 && algo <= maxAlgoEnumInt)
                                {
                                    package.PresetAlgoType = (AlgoType)(algo - 1);
                                }
                            }
                            else if (Enum.TryParse(option.Algo, true, out AlgoType algoType))
                            {
                                package.PresetAlgoType = algoType;
                            }
                        }
                        this.viewModel.BeginDisplayModels(package);
                    }
                })
                .WithParsed<VerifyHash>(option =>
                {
                    if (File.Exists(option.BasisPath))
                    {
                        HashBasis newBasis = new HashBasis(option.BasisPath);
                        if (newBasis.ReasonForFailure == null)
                        {
                            this.viewModel.BeginDisplayModels(
                                new PathPackage(Path.GetDirectoryName(option.BasisPath),
                                    Settings.Current.SelectedQVSPolicy, newBasis));
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, newBasis.ReasonForFailure, "错误",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    }
                });
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
        private void ComputeCrossProcessFiles()
        {
            MappedFiler.ExistingProcessId = ProcessId;
            while (true)
            {
                MappedFiler.Synchronizer.Wait();
                // ToArray 能避免 GetArgs 方法在 ParseArguments 内被执行多次
                string[] args = MappedFiler.GetArgs().ToArray();
                this.InternalParseArguments(args);
            }
        }

        private void ProcessIdMonitorProc()
        {
            while (true)
            {
                MappedFiler.PIdSynchronizer.Wait();
                if (!this.ProcIdMonitorFlag)
                {
                    MappedFiler.PIdSynchronizer.Set();
                    break;
                }
                Thread thread = new Thread(this.ComputeCrossProcessFiles);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void DataGridHashingFilesDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                !(e.Data.GetData(DataFormats.FileDrop) is string[] data) || !data.Any())
            {
                return;
            }
            this.viewModel.BeginDisplayModels(
                new PathPackage(data, Settings.Current.SelectedSearchPolicy));
        }

        private void DataGridHashingFilesPrevKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && sender is DataGrid dataGrid)
            {
                dataGrid.SelectedItem = null;
            }
        }

        private void ButtonSelectBasisFileSetPathClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFile = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (openFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Current.LastUsedPath = Path.GetDirectoryName(openFile.FileName);
                this.viewModel.HashStringOrBasisPath = openFile.FileName;
            }
        }

        private void TextBoxHashOrFilePathPreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                !(e.Data.GetData(DataFormats.FileDrop) is string[] data) || !data.Any())
            {
                return;
            }
            this.viewModel.HashStringOrBasisPath = data[0];
        }

        private void TextBoxHashValueOrFilePathPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
