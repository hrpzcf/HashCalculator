using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Basis VerificationBasis = new Basis();
        private readonly List<ModelArg> FilesDroppedToHash = new List<ModelArg>();
        private readonly ObservableCollection<HashModel> HashModels =
            new ObservableCollection<HashModel>();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = $"{Info.Title} v{Info.Ver} — {Info.Author} @ {Info.Published}";
            this.uiDataGrid_HashFiles.ItemsSource = this.HashModels;
            this.InitFromConfigure();
            ModelTaskHelper.StartingEvent += this.ActionGroupStarting;
            ModelTaskHelper.IncreaseEvent += this.ProgressInfoUpdate;
            ModelTaskHelper.FinishedEvent += this.ActionGroupCompleted;
            ModelTaskHelper.MaxCountEvent += this.ActionGroupMaxCount;
            ModelTaskHelper.RefreshTaskLimit();
            ModelTaskHelper.InitializeHelper(this.HashModels.Add);
        }

        private void InitFromConfigure()
        {
            Configure config = Settings.Current;
            this.Topmost = config.MainWindowTopmost;
            this.uiCheckBox_WindowTopMost.IsChecked = config.MainWindowTopmost;
            this.uiComboBox_HashAlgorithm.SelectedIndex = (int)config.SelectedAlgo;
            this.uiComboBox_HashAlgorithm.SelectionChanged +=
                this.ComboBox_HashAlgorithm_SelectionChanged;
            if (config.RemMainWindowPosition)
            {
                this.Top = config.MainWindowTop;
                this.Left = config.MainWindowLeft;
            }
            if (config.RembMainWindowSize)
            {
                this.Width = config.MainWindowWidth;
                this.Height = config.MainWindowHeight;
            }
        }

        private void Window_MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Configure config = Settings.Current;
            if (config.RemMainWindowPosition)
            {
                config.MainWindowTop = this.Top;
                config.MainWindowLeft = this.Left;
            }
            if (config.RembMainWindowSize)
            {
                config.MainWindowWidth = this.Width;
                config.MainWindowHeight = this.Height;
            }
            Settings.SaveConfigure();
        }

        private void SearchUnderSpecifiedPolicy(IEnumerable<string> paths, List<string> outDataPaths)
        {
            switch (Settings.Current.DroppedSearchPolicy)
            {
                default:
                case SearchPolicy.Children:
                    foreach (string p in paths)
                    {
                        if (Directory.Exists(p))
                        {
                            DirectoryInfo di = new DirectoryInfo(p);
                            outDataPaths.AddRange(di.GetFiles().Select(i => i.FullName));
                        }
                        else if (File.Exists(p)) outDataPaths.Add(p);
                    }
                    break;
                case SearchPolicy.Descendants:
                    foreach (string p in paths)
                    {
                        if (Directory.Exists(p))
                        {
                            DirectoryInfo di = new DirectoryInfo(p);
                            outDataPaths.AddRange(
                                di.GetFiles("*", SearchOption.AllDirectories).Select(i => i.FullName)
                            );
                        }
                        else if (File.Exists(p)) outDataPaths.Add(p);
                    }
                    break;
                case SearchPolicy.DontSearch:
                    foreach (string p in paths) if (File.Exists(p)) outDataPaths.Add(p);
                    break;
            }
        }

        private void DataGrid_FilesToCalculate_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            List<string> searchedPaths = new List<string>();
            this.SearchUnderSpecifiedPolicy(data, searchedPaths);
            if (searchedPaths.Count == 0)
                return;
            IEnumerable<ModelArg> modelArgs = searchedPaths.Select(s => new ModelArg(s));
            this.FilesDroppedToHash.AddRange(modelArgs);
            this.NewThreadAndEnqueueFilePaths(modelArgs);
        }

        private void NewThreadAndEnqueueFilePaths(IEnumerable<ModelArg> args)
        {
            ModelArg[] modelargs = args.ToArray();
            for (int i = 0; i < modelargs.Length; ++i)
                modelargs[i].tokenSrc = new CancellationTokenSource();
            ModelTaskHelper.CounterMax(args.Count());
            ModelTaskHelper.SendRequestToQueueArgs();
            new Thread(() =>
            {
                ModelTaskHelper.QueueModelArgs(modelargs);
            })
            { IsBackground = true }.Start();
        }

        private void ProgressInfoShow()
        {
            this.uiButton_RefreshCopy.IsEnabled = false;
            this.uiButton_RefreshCurrentHash.IsEnabled = false;
            this.uiButton_StartCompare.IsEnabled = false;
            this.uiButton_ClearModels.IsEnabled = false;
            this.uiButton_ExportAsText.IsEnabled = false;
            this.uiTextBlock_CompletedTotal.Visibility = Visibility.Visible;
            this.uiTextBlock_CompletedCount.Text = "0";
            this.uiTextBlock_CompletedCount.Visibility = Visibility.Visible;
            this.uiTextBlock_Delimiter.Visibility = Visibility.Visible;
            this.uiTextBlock_TotalTaskCount.Visibility = Visibility.Visible;
            this.uiProgressbar_TaskProgress.Value = 0;
            this.uiProgressbar_TaskProgress.Visibility = Visibility.Visible;
        }

        private void ProgressInfoHide()
        {
            this.uiButton_RefreshCopy.IsEnabled = true;
            this.uiButton_RefreshCurrentHash.IsEnabled = true;
            this.uiButton_StartCompare.IsEnabled = true;
            this.uiButton_ClearModels.IsEnabled = true;
            this.uiButton_ExportAsText.IsEnabled = true;
            this.uiTextBlock_CompletedTotal.Visibility = Visibility.Hidden;
            this.uiTextBlock_CompletedCount.Visibility = Visibility.Hidden;
            this.uiTextBlock_Delimiter.Visibility = Visibility.Hidden;
            this.uiTextBlock_TotalTaskCount.Visibility = Visibility.Hidden;
            this.uiProgressbar_TaskProgress.Visibility = Visibility.Hidden;
        }

        private void ActionGroupStarting(int completed)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.ProgressInfoShow();
                ToolTipReport.Instance.Report = "暂无校验报告";
            });
        }

        private void ProgressInfoUpdate(int completed)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.uiProgressbar_TaskProgress.Value = completed;
                this.uiTextBlock_CompletedCount.Text = $"{completed}";
            });
        }

        private void ActionGroupCompleted(int completed)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.ProgressInfoHide();
                this.GenerateVerificationReport();
            });
        }

        private void ActionGroupMaxCount(int maxCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.uiProgressbar_TaskProgress.Maximum = maxCount;
                this.uiTextBlock_TotalTaskCount.Text = $"{maxCount}";
            });
        }

        private void RefreshCurrentDataGridLines(bool clearLines)
        {
            if (clearLines)
            {
                this.HashModels.Clear();
                SerialGenerator.Reset();
            }
            this.NewThreadAndEnqueueFilePaths(this.FilesDroppedToHash);
        }

        private void Button_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.HashModels.Clear();
            this.FilesDroppedToHash.Clear();
            SerialGenerator.Reset();
        }

        private void Button_ExportAsTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.HashModels.Count == 0)
            {
                MessageBox.Show("列表中没有任何需要导出的条目。", "提示");
                return;
            }
            Configure config = Settings.Current;
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = "hashsums.txt",
                InitialDirectory = config.SavedDirPath,
            };
            if (sf.ShowDialog() != true)
                return;
            config.SavedDirPath = Path.GetDirectoryName(sf.FileName);
            //Settings.SaveConfigure(); // 窗口关闭时会 SaveConfigure
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (HashModel hm in this.HashModels)
                        if (hm.IsCompleted && hm.Export)
                        {
                            sw.WriteLine($"{hm.Hash} *{hm.Name}");
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"结果导出为文本文件失败：\n{ex.Message}", "错误");
                return;
            }
        }

        private void GlobalUpdateHashNameItems(string pathOrHash)
        {
            this.VerificationBasis.Clear();
            if (this.uiComboBox_ComparisonMethod.SelectedIndex == 0)
            {
                if (!File.Exists(pathOrHash))
                {
                    MessageBox.Show("校验依据来源文件不存在。", "提示");
                    return;
                }
                try
                {
                    foreach (string line in File.ReadAllLines(pathOrHash))
                    {
                        string[] items = line.Split(
                            new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (items.Length < 2)
                        {
                            if (MessageBox.Show(
                                    "哈希值文件行读取错误，可能该行格式不正确，是否继续？",
                                    "错误",
                                    MessageBoxButton.YesNo) == MessageBoxResult.No)
                                return;
                            else
                                continue;
                        }
                        this.VerificationBasis.Add(items);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"校验依据来源文件打开失败：\n{ex.Message}", "错误");
                    return;
                }
            }
            else
                this.VerificationBasis.Add(new string[] { pathOrHash.Trim(), "" });
        }

        private void CheckWithExpectedHash(string path)
        {
            FileInfo[] infosInSameFolder;
            string expectedHash;
            FileInfo hashValueFileInfo = new FileInfo(path);
            if (Settings.Current.QuickVerificationSearchPolicy == SearchPolicy.Children)
                infosInSameFolder = hashValueFileInfo.Directory.GetFiles();
            else if (Settings.Current.QuickVerificationSearchPolicy == SearchPolicy.Descendants)
                infosInSameFolder = hashValueFileInfo.Directory.GetFiles(
                    "*", SearchOption.AllDirectories);
            else
            {
                MessageBox.Show(
                    "设置项\"当使用快速校验时\"选择了不搜索文件夹，快速校验无法完成，请在设置面板选择合适选项。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            Dictionary<string, List<string>>
                nameFullName = new Dictionary<string, List<string>>();
            foreach (FileInfo info in infosInSameFolder)
            {
                string keylower = info.Name.ToLower();
                if (!nameFullName.ContainsKey(keylower))
                    nameFullName[keylower] = new List<string> { info.FullName };
                else
                    nameFullName[keylower].Add(info.FullName);
            }
            try
            {
                Dictionary<string, List<string>> verificationBasis
                    = new Dictionary<string, List<string>>();
                List<ModelArg> hashModelArgsList = new List<ModelArg>();
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] hashfname = line.Split(
                            new char[] { ' ' },
                            2,
                            StringSplitOptions.RemoveEmptyEntries);
                    if (hashfname.Length < 2)
                        if (MessageBox.Show(
                                "哈希值文件行读取错误，可能该行格式不正确，是否继续？",
                                "错误",
                                MessageBoxButton.YesNo) == MessageBoxResult.No)
                            return;
                        else
                            continue;
                    hashfname[1] = hashfname[1].Trim(new char[] { '*', ' ', '\n' });
                    string namelower = hashfname[1].ToLower();
                    if (verificationBasis.ContainsKey(namelower))
                        verificationBasis[namelower].Add(hashfname[0]);
                    else
                        verificationBasis[namelower] = new List<string> { hashfname[0] };
                }
                foreach (KeyValuePair<string, List<string>> pair in verificationBasis)
                    if (nameFullName.ContainsKey(pair.Key))
                    {
                        string first = pair.Value.First();
                        if (pair.Value.Count > 1)
                            expectedHash = pair.Value.All(s => s == first) ? first : "";
                        else
                            expectedHash = first;
                        foreach (string fullpath in nameFullName[pair.Key])
                            hashModelArgsList.Add(new ModelArg(expectedHash, fullpath));
                    }
                    else
                        hashModelArgsList.Add(new ModelArg(pair.Key, true));
                if (hashModelArgsList.Count == 0)
                {
                    MessageBox.Show(
                        "校验依据内容为空或搜索到的文件数量为零，请检查设置面板中的\"当使用快速校验时\"选项。",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
                this.FilesDroppedToHash.AddRange(hashModelArgsList);
                this.NewThreadAndEnqueueFilePaths(hashModelArgsList);
            }
            catch { return; }
        }

        private void GenerateVerificationReport()
        {
            int noresult, unrelated, matched, mismatch, uncertain;
            noresult = unrelated = matched = mismatch = uncertain = 0;
            foreach (HashModel hm in this.HashModels)
            {
                switch (hm.CmpResult)
                {
                    case CmpRes.NoResult:
                        ++noresult;
                        break;
                    case CmpRes.Unrelated:
                        ++unrelated;
                        break;
                    case CmpRes.Matched:
                        ++matched;
                        break;
                    case CmpRes.Mismatch:
                        ++mismatch;
                        break;
                    case CmpRes.Uncertain:
                        ++uncertain;
                        break;
                }
            }
            ToolTipReport.Instance.Report
                = $"校验报告：\n\n已匹配：{matched}\n"
                + $"不匹配：{mismatch}\n"
                + $"不确定：{uncertain}\n"
                + $"无关联：{unrelated}\n"
                + $"未校验：{noresult} \n\n"
                + $"队列总数：{this.HashModels.Count}";
        }

        private void AcceptNewFilePathsLockButtons()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.uiButton_SelectFilesToHash.IsEnabled = false;
                this.uiButton_SelectFilesToHash.Content = "稍候...";
                this.uiDataGrid_HashFiles.AllowDrop = false;
                this.uiButton_SelectFoldersToHash.IsEnabled = false;
                this.uiButton_SelectFoldersToHash.Content = "稍候...";
                this.uiButton_StartCompare.IsEnabled = false;
                this.uiButton_StartCompare.Content = "稍候...";
            });
        }

        private void AcceptNewFilePathsReleaseButtons()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.uiButton_SelectFilesToHash.IsEnabled = true;
                this.uiButton_SelectFilesToHash.Content = "选择文件";
                this.uiDataGrid_HashFiles.AllowDrop = true;
                this.uiButton_SelectFoldersToHash.IsEnabled = true;
                this.uiButton_SelectFoldersToHash.Content = "选择文件夹";
                this.uiButton_StartCompare.Content = "校验";
            });
        }

        private void Button_StartCompare_Click(object sender, RoutedEventArgs e)
        {
            string pathOrHash = this.uiTextBox_HashValueOrFilePath.Text;
            if (string.IsNullOrEmpty(pathOrHash))
            {
                MessageBox.Show("未输入哈希值校验依据。", "提示");
                return;
            }
            if (this.HashModels.Count == 0 && File.Exists(pathOrHash))
            {
                this.AcceptNewFilePathsLockButtons();
                new Thread(() =>
                {
                    this.CheckWithExpectedHash(pathOrHash);
                    this.AcceptNewFilePathsReleaseButtons();
                })
                { IsBackground = true }.Start();
                // 因为要开新线程计算哈希值，所以
                // 此处 GenerateVerificationReport 移到 UpdateProgress 中
            }
            else
            {
                this.GlobalUpdateHashNameItems(pathOrHash);
                foreach (HashModel hm in this.HashModels)
                {
                    if (hm.IsCompleted)
                        hm.CmpResult = this.VerificationBasis.Verify(hm.Name, hm.Hash);
                    else
                        hm.CmpResult = CmpRes.NoResult;
                }
                this.GenerateVerificationReport();
            }
        }

        private void TextBox_HashValueOrFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBox_HashValueOrFilePath_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            this.uiTextBox_HashValueOrFilePath.Text = data[0];
        }

        private void TextBox_HashValueOrFilePath_Changed(object sender, TextChangedEventArgs e)
        {
            this.uiComboBox_ComparisonMethod.SelectedIndex =
                File.Exists(this.uiTextBox_HashValueOrFilePath.Text) ? 0 : 1;
        }

        private void CheckBox_WindowTopmost_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = this.uiCheckBox_WindowTopMost.IsChecked == true;
            Settings.Current.MainWindowTopmost = this.Topmost;
            //Settings.SaveConfigure(); // 窗口关闭时会 SaveConfigure
        }

        private void ComboBox_HashAlgorithm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (Locks.AlgoSelectionLock)
            {
                Settings.Current.SelectedAlgo = (AlgoType)this.uiComboBox_HashAlgorithm.SelectedIndex;
                //Settings.SaveConfigure(); // 窗口关闭时会 SaveConfigure
            }
        }

        private void Button_CopyHashValue_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HashModel hashModel = button.DataContext as HashModel;
            Clipboard.SetText(hashModel.Hash);
        }

        private void Button_CopyRefreshHash_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshCurrentDataGridLines(false);
        }

        private void Button_RefreshCurrentHash_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshCurrentDataGridLines(true);
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
            Configure config = Settings.Current;
            CommonOpenFileDialog openFile = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = config.SavedDirPath,
            };
            if (openFile.ShowDialog() == CommonFileDialogResult.Ok)
            {
                config.SavedDirPath = Path.GetDirectoryName(openFile.FileName);
                this.uiTextBox_HashValueOrFilePath.Text = openFile.FileName;
                // TextBox_HashValueOrFilePath_Changed 已实现
                //this.uiComboBox_ComparisonMethod.SelectedIndex = 0;
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
            ModelTaskHelper.CancelTasks();
        }

        private void Button_SelectFilesToHash_Click(object sender, RoutedEventArgs e)
        {
            Configure config = Settings.Current;
            CommonOpenFileDialog fileOpen = new CommonOpenFileDialog
            {
                Title = "选择文件",
                InitialDirectory = config.SavedDirPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (fileOpen.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (fileOpen.FileNames.Count() == 0)
                {
                    MessageBox.Show("没有选择任何文件", "提示");
                    return;
                }
                config.SavedDirPath = Path.GetDirectoryName(fileOpen.FileNames.ElementAt(0));
                this.AcceptNewFilePathsLockButtons();
                IEnumerable<ModelArg> modelArgs = fileOpen.FileNames.Select(s => new ModelArg(s));
                this.FilesDroppedToHash.AddRange(modelArgs);
                this.NewThreadAndEnqueueFilePaths(modelArgs);
                this.AcceptNewFilePathsReleaseButtons();
            }
        }

        private void Button_SelectFoldersToHash_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Current.DroppedSearchPolicy == SearchPolicy.DontSearch)
            {
                MessageBox.Show(
                    "当前设置中的文件夹搜索策略为\"不搜索该文件夹\"，此按钮无法获取文件夹下的文件，请更改为其他选项。",
                    "提示");
                return;
            }
            Configure config = Settings.Current;
            CommonOpenFileDialog folderOpen = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = config.SavedDirPath,
                Multiselect = true,
                EnsureValidNames = true,
            };
            if (folderOpen.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (folderOpen.FileNames.Count() == 0)
                {
                    MessageBox.Show("没有选择任何文件夹", "提示");
                    return;
                }
                config.SavedDirPath = folderOpen.FileNames.ElementAt(0);
                List<string> folderPaths = new List<string>();
                this.AcceptNewFilePathsLockButtons();
                new Thread(() =>
                {
                    this.SearchUnderSpecifiedPolicy(folderOpen.FileNames, folderPaths);
                    IEnumerable<ModelArg> modelArgs = folderPaths.Select(s => new ModelArg(s));
                    this.FilesDroppedToHash.AddRange(modelArgs);
                    this.NewThreadAndEnqueueFilePaths(modelArgs);
                    this.AcceptNewFilePathsReleaseButtons();
                })
                { IsBackground = true }.Start();
            }
        }
    }
}
