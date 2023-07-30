using CommandLine;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private readonly MainWndViewModel viewModel = new MainWndViewModel();
        private static readonly int maxAlgoEnumInt =
            Enum.GetNames(typeof(AlgoType)).Length - 1;
        private static string[] startupArgs = null;
        private static readonly int curProcId = Process.GetCurrentProcess().Id;

        private bool ProcIdMonitorFlag { get; set; } = true;

        public static MainWindow This { get; private set; }

        public static IntPtr WndHandle { get; private set; }

        public MainWindow()
        {
            This = this;
            this.viewModel.Parent = this;
            this.DataContext = this.viewModel;
            this.Closed += this.MainWindowClosed;
            this.Loaded += this.MainWindowLoaded;
            this.InitializeComponent();
            this.Title = $"{Info.Title} v{Info.Ver} by {Info.Author} @ {Info.Published}";
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
                            else if (Enum.TryParse(option.Algo.ToUpper(), out AlgoType algoType))
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
            MappedFiler.ExistingProcessId = curProcId;
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

        private void MainWindowClosed(object sender, EventArgs e)
        {
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

        private void DataGrid_FilesToCalculate_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || !data.Any())
            {
                return;
            }
            this.viewModel.BeginDisplayModels(
                new PathPackage(data, Settings.Current.SelectedSearchPolicy));
        }

        private void DataGrid_HashFiles_PrevKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.uiDataGrid_HashFiles.SelectedIndex = -1;
            }
        }

        private void Button_SelectBasisFileSetPath_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFile = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (openFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Current.LastUsedPath = Path.GetDirectoryName(openFile.FileName);
                this.uiTextBox_HashValueOrFilePath.Text = openFile.FileName;
            }
        }

        private void TextBox_HashOrFilePath_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || !data.Any())
            {
                return;
            }
            this.uiTextBox_HashValueOrFilePath.Text = data[0];
        }

        private void TextBox_HashValueOrFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
