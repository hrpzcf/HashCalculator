using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private readonly MainWndViewModel viewModel = new MainWndViewModel();

        public static MainWindow This { get; private set; }

        public static IntPtr WndHandle { get; private set; }

        public static IEnumerable<string> PathsFromStartupArgs { get; set; }

        public MainWindow()
        {
            This = this;
            this.viewModel.Parent = this;
            this.DataContext = this.viewModel;
            this.Loaded += this.MainWindowLoaded;
            this.InitializeComponent();
            this.Title = $"{Info.Title} v{Info.Ver} by {Info.Author} @ {Info.Published}";
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            WndHandle = new WindowInteropHelper(this).Handle;
            if (PathsFromStartupArgs != null)
            {
                this.viewModel.BeginDisplayModels(
                    new PathPackage(PathsFromStartupArgs, Settings.Current.SelectedSearchPolicy));
                PathsFromStartupArgs = default;
            }
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
