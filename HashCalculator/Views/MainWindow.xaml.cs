using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
        private readonly Basis VerificationBasis = new Basis();
        private readonly MainWndViewModel viewModel = new MainWndViewModel();

        public static MainWindow This { get; private set; }

        public static IntPtr WndHandle { get; private set; }

        public static ScrollViewer DataGridScroll { get; private set; }

        public MainWindow()
        {
            this.DataContext = this.viewModel;
            this.Loaded += this.MainWindowLoaded;
            this.InitializeComponent();
            this.Title = $"{Info.Title} v{Info.Ver} by {Info.Author} @ {Info.Published}";
            this.viewModel.ChangeTaskNumber(Settings.Current.SelectedTaskNumberLimit);
            This = this;
            this.VerificationBasis.Parent = this;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            WndHandle = new WindowInteropHelper(this).Handle;
            if (VisualTreeHelper.GetChild(this.uiDataGrid_HashFiles, 0) is Border border)
            {
                DataGridScroll = border.Child as ScrollViewer;
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
            this.viewModel.BeginDisplayModels(new PathPackage(data, Settings.Current.SelectedSearchPolicy));
        }

        private void Button_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.ClearHashViewModels();
        }

        private void Button_ExportAsTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (!this.viewModel.HashViewModels.Any())
            {
                MessageBox.Show(this, "列表中没有任何可以导出的条目。", "提示");
                return;
            }
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = "hashsums.txt",
                InitialDirectory = Settings.Current.LastUsedPath,
            };
            if (sf.ShowDialog() != true)
            {
                return;
            }
            Settings.Current.LastUsedPath = Path.GetDirectoryName(sf.FileName);
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (HashViewModel hm in this.viewModel.HashViewModels)
                    {
                        if (hm.IsSucceeded && hm.Export)
                        {
                            string hash = HashBytesOutputTypeCvt.Convert(
                                hm.Hash, Settings.Current.SelectedOutputType) as string;
                            sw.WriteLine($"{hash} *{hm.FileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"哈希值导出失败：\n{ex.Message}", "错误");
                return;
            }
        }

        private void Button_StartCompare_Click(object sender, RoutedEventArgs e)
        {
            string pathOrHash = this.uiTextBox_HashValueOrFilePath.Text;
            if (string.IsNullOrEmpty(pathOrHash))
            {
                MessageBox.Show(this, "没有输入哈希值校验依据。", "提示");
                return;
            }
            bool updated = false;
            if (this.uiComboBox_ComparisonMethod.SelectedIndex == 0)
            {
                if (!File.Exists(pathOrHash))
                {
                    MessageBox.Show(this, "校验依据来源文件不存在。", "提示");
                    return;
                }
                else
                {
                    if (!this.viewModel.HashViewModels.Any())
                    {
                        Basis verificationBasis = new Basis(pathOrHash);
                        this.viewModel.BeginDisplayModels(
                            new PathPackage(new string[] { Path.GetDirectoryName(pathOrHash) },
                                Settings.Current.SelectedQVSPolicy, verificationBasis));
                        return;
                    }
                    else
                    {
                        updated = this.VerificationBasis.UpdateWithFile(pathOrHash);
                    }
                }
            }
            else if (this.uiComboBox_ComparisonMethod.SelectedIndex == 1)
            {
                updated = this.VerificationBasis.UpdateWithHash(pathOrHash);
            }
            if (!updated)
            {
                return;
            }
            foreach (HashViewModel hm in this.viewModel.HashViewModels)
            {
                if (hm.IsSucceeded)
                {
                    hm.CmpResult = this.VerificationBasis.Verify(hm.FileName, hm.Hash);
                }
                else
                {
                    hm.CmpResult = CmpRes.NoResult;
                }
            }
            this.viewModel.GenerateVerificationReport();
        }

        private void TextBox_HashValueOrFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBox_HashValueOrFilePath_PreviewDrop(object sender, DragEventArgs e)
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

        private void TextBox_HashValueOrFilePath_Changed(object sender, TextChangedEventArgs e)
        {
            this.uiComboBox_ComparisonMethod.SelectedIndex =
                File.Exists(this.uiTextBox_HashValueOrFilePath.Text) ? 0 : 1;
        }

        private void Button_CopyRefreshHash_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_Restart(true, false);
        }

        private void Button_RefreshCurrentHash_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_Restart(false, false);
        }

        private void Button_RefreshCurrentHashForce_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_Restart(false, true);
        }

        private void MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new SettingsPanel() { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void DataGrid_HashFiles_PrevKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.uiDataGrid_HashFiles.SelectedIndex = -1;
            }
        }

        private void Button_SelectHashSetFile_Click(object sender, RoutedEventArgs e)
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

        private void MenuItem_UsingHelp_Click(object sender, RoutedEventArgs e)
        {
            Window usingHelpWindow = new UsingHelpWindow()
            {
                Owner = this,
            };
            usingHelpWindow.ShowDialog();
        }

        private void Button_CancelAllTask_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_CancelAll();
        }

        private void Button_SelectFilesToHash_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog fileOpen = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = Settings.Current.LastUsedPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (fileOpen.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Settings.Current.LastUsedPath =
                    Path.GetDirectoryName(fileOpen.FileNames.ElementAt(0));
            this.viewModel.BeginDisplayModels(
                new PathPackage(fileOpen.FileNames, Settings.Current.SelectedSearchPolicy));
        }

        private void Button_SelectFoldersToHash_Click(object sender, RoutedEventArgs e)
        {
            SearchPolicy policy = Settings.Current.SelectedSearchPolicy;
            if (policy == SearchPolicy.DontSearch)
            {
                policy = SearchPolicy.Children;
            }
            CommonOpenFileDialog folderOpen = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = Settings.Current.LastUsedPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (folderOpen.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Settings.Current.LastUsedPath = folderOpen.FileNames.ElementAt(0);
            this.viewModel.BeginDisplayModels(new PathPackage(folderOpen.FileNames, policy));
        }

        private void Button_CancelHashModel_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HashViewModel hashModel = button.DataContext as HashViewModel;
            this.viewModel.Models_CancelOne(hashModel);
        }

        private void Button_RestartHashModel_CLick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HashViewModel hashModel = button.DataContext as HashViewModel;
            this.viewModel.Models_StartOne(hashModel);
        }

        private void Button_PauseHashModel_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HashViewModel hashModel = button.DataContext as HashViewModel;
            this.viewModel.Models_PauseOne(hashModel);
        }

        private void Button_PauseAllTask_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_PauseAll();
        }

        private void Button_ContinueAllTask_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.Models_ContinueAll();
        }

        private void Button_WindowTopmost_Click(object sender, RoutedEventArgs e)
        {
            Settings.Current.MainWndTopmost = !Settings.Current.MainWndTopmost;
        }
    }
}
