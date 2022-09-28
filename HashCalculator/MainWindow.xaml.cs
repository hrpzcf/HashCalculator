using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace HashCalculator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<HashModel> HashModels =
            new ObservableCollection<HashModel>();
        private int QueuedFilesCount = 0;
        private readonly Queue<ModelArg> FilesToHashQueue = new Queue<ModelArg>();
        private readonly Action<Task> ActUpdateProgress;
        private readonly Basis basis = new Basis();
        private readonly List<CancellationTokenSource> cancelTokenSrcs
            = new List<CancellationTokenSource>();
        private readonly List<ModelArg> FilesDroppedToHash = new List<ModelArg>();

        public MainWindow()
        {
            this.InitializeComponent();
            this.uiDataGrid_HashFiles.ItemsSource = this.HashModels;
            this.Title = $"{Info.Title} v{Info.Ver} — {Info.Author} @ {Info.Published}";
            this.ActUpdateProgress = new Action<Task>(this.UpdateProgress);
            new Thread(this.ThreadAddHashModel) { IsBackground = true }.Start();
            this.InitializeFromConfigure();
        }

        private void InitializeFromConfigure()
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

        private void SearchUnderSpecifiedPolicy(string[] paths, List<string> outDataPaths)
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
            this.CreateTaskToEnqueueFilePaths(modelArgs);
        }

        private void CreateTaskToEnqueueFilePaths(IEnumerable<ModelArg> modelArgs)
        {
            Task.Run(() =>
            {
                lock (Locks.MainLock)
                {
                    foreach (ModelArg arg in modelArgs)
                        this.FilesToHashQueue.Enqueue(arg);
                    this.QueuedFilesCount += modelArgs.Count();
                    Application.Current.Dispatcher.Invoke(this.AfterFilesQueued);
                }
            });
        }

        private void ShowProgressInfo()
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

        private void HideProgressInfo()
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

        private void AfterFilesQueued()
        {
            if (this.QueuedFilesCount < 1)
                return;
            this.uiProgressbar_TaskProgress.Maximum = this.QueuedFilesCount;
            this.uiTextBlock_TotalTaskCount.Text = this.QueuedFilesCount.ToString();
            if (CompletionCounter.Count() <= 0)
            {
                ToolTipReport.Instance.Report = "暂无校验报告";
                this.ShowProgressInfo();
                Locks.UpdateComputeLock(); // 更新“同时计算多少个文件”的信号量
            }
        }

        private void AddHashModel(int serial, ModelArg modelArg)
        {
            CancellationTokenSource tokenSrc = new CancellationTokenSource();
            this.cancelTokenSrcs.Add(tokenSrc);
            modelArg.cancelToken = tokenSrc.Token;
            HashModel hashModel = new HashModel(serial, modelArg);
            this.HashModels.Add(hashModel);
            tokenSrc.Token.Register(hashModel.CancelledCallback);
            Task.Run(hashModel.EnterGenerateUnderLimit, tokenSrc.Token)
                .ContinueWith(this.ActUpdateProgress);
        }

        private void UpdateProgress(Task task)
        {
            int completed = CompletionCounter.Count();
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    this.uiProgressbar_TaskProgress.Value = completed;
                    this.uiTextBlock_CompletedCount.Text = completed.ToString();
                }
            );
            if (completed >= this.QueuedFilesCount)
            {
                this.cancelTokenSrcs.Clear();
                CompletionCounter.ResetCount();
                this.QueuedFilesCount = 0;
                Application.Current.Dispatcher.Invoke(this.HideProgressInfo);
                Application.Current.Dispatcher.Invoke(this.GenerateVerificationReport);
            }
        }

        private void RefreshCurrentDataGridLines(bool clearResults)
        {

            if (clearResults)
            {
                lock (Locks.MainLock)
                {
                    this.HashModels.Clear(); SerialGenerator.Reset();
                }
            }
            this.CreateTaskToEnqueueFilePaths(this.FilesDroppedToHash);
        }

        private void ThreadAddHashModel()
        {
            int serial;
            Action<int, ModelArg> ahm = new Action<int, ModelArg>(this.AddHashModel);
            while (true)
            {
                lock (Locks.MainLock)
                {
                    if (this.FilesToHashQueue.Count == 0)
                        goto pause;
                    ModelArg arg = this.FilesToHashQueue.Dequeue();
                    serial = SerialGenerator.GetSerial();
                    Application.Current.Dispatcher.Invoke(ahm, serial, arg);
                }
            pause:
                Thread.Sleep(10);
            }
        }

        private void Button_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.cancelTokenSrcs.Clear();
            this.HashModels.Clear();
            this.FilesDroppedToHash.Clear();
            SerialGenerator.Reset();
            this.QueuedFilesCount = 0;
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
                InitialDirectory = config.SavedPath,
            };
            if (sf.ShowDialog() != true)
                return;
            config.SavedPath = Path.GetDirectoryName(sf.FileName);
            //Settings.SaveConfigure(); // 窗口关闭时会 SaveConfigure
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (HashModel hm in this.HashModels)
                        if (hm.Completed && hm.Export)
                            sw.WriteLine($"{hm.Hash} *{hm.Name}");
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
            this.basis.Clear();
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
                        this.basis.Add(items);
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
                this.basis.Add(new string[] { pathOrHash.Trim(), "" });
        }

        private void CompareWithExpectedHash(string path)
        {
            FileInfo hashValueFileInfo = new FileInfo(path);
            FileInfo[] infosInSameFolder;
            if (Settings.Current.UsingQuickVerification == SearchPolicy.Children)
                infosInSameFolder = hashValueFileInfo.Directory.GetFiles();
            else if (Settings.Current.UsingQuickVerification == SearchPolicy.Descendants)
                infosInSameFolder = hashValueFileInfo.Directory.GetFiles(
                    "*", SearchOption.AllDirectories);
            else
            {
                MessageBox.Show(
                    "设置项\"当使用快速校验时\"选择了不搜索" +
                    "文件夹，快速校验无法完成，请在设置面板选择合适选项。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            try
            {
                bool fileFound = false;
                List<ModelArg> hashModelArgsList = new List<ModelArg>();
                foreach (string line in File.ReadAllLines(path))
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
                    items[1] = items[1].Trim(new char[] { '*', ' ', '\n' });
                    fileFound = false;
                    foreach (FileInfo fi in infosInSameFolder)
                    {
                        if (items[1].ToLower() == fi.Name.ToLower())
                        {
                            items[1] = fi.FullName;
                            hashModelArgsList.Add(new ModelArg(items));
                            fileFound = true;
                        }
                    }
                    if (!fileFound)
                        hashModelArgsList.Add(new ModelArg(items[1], true));
                }
                if (hashModelArgsList.Count == 0)
                    return;
                this.FilesDroppedToHash.AddRange(hashModelArgsList);
                this.CreateTaskToEnqueueFilePaths(hashModelArgsList);
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
                + $"文件总数：{this.HashModels.Count}";
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
                this.CompareWithExpectedHash(pathOrHash);
                // 因为要新线程计算哈希值，所以
                // 此处 GenerateVerificationReport 移到 UpdateProgress 中
            }
            else
            {
                this.GlobalUpdateHashNameItems(pathOrHash);
                foreach (HashModel hm in this.HashModels)
                {
                    if (hm.Completed)
                        hm.CmpResult = this.basis.Verify(hm.Name, hm.Hash);
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
            OpenFileDialog askUserFilePath = new OpenFileDialog
            {
                Filter = "所有文件|*.*",
            };
            if (askUserFilePath.ShowDialog() == true)
            {
                this.uiTextBox_HashValueOrFilePath.Text = askUserFilePath.FileName;
                // TextBox_HashValueOrFilePath_Changed 已实现
                //this.uiComboBox_ComparisonMethod.SelectedIndex = 0;
            }
        }

        private void MenuItem_UsingHelp_Click(object sender, RoutedEventArgs e)
        {
            Window usingHelpWindow = new UsingHelp()
            {
                Owner = this,
            };
            usingHelpWindow.ShowDialog();
        }

        private void Button_CancelAllTask_Click(object sender, RoutedEventArgs e)
        {
            lock (Locks.MainLock)
            {
                this.QueuedFilesCount -= this.FilesToHashQueue.Count;
                this.FilesToHashQueue.Clear();
            }
            foreach (CancellationTokenSource cancelSrc in this.cancelTokenSrcs)
                cancelSrc.Cancel();
            this.UpdateProgress(null);
            MessageBox.Show("正在执行的任务和排队中的任务已全部取消。", "任务已取消");
        }

        private void Button_SelectFilesToHash_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog askFilePaths = new OpenFileDialog
            {
                Filter = "所有文件|*.*",
                DereferenceLinks = false,
                Multiselect = true,
                ValidateNames = true,
            };
            if (askFilePaths.ShowDialog() == true)
            {
                IEnumerable<ModelArg> modelArgs =
                    askFilePaths.FileNames.Select(s => new ModelArg(s));
                this.FilesDroppedToHash.AddRange(modelArgs);
                this.CreateTaskToEnqueueFilePaths(modelArgs);
            }
        }

        private void Button_SelectFoldersToHash_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Current.DroppedSearchPolicy == SearchPolicy.DontSearch)
            {
                MessageBox.Show("当前设置中的文件夹搜索策略为" +
                    "\"不搜索该文件夹\"，此按钮无法获取文件夹下的文件，请更改为其他选项。", "提示");
                return;
            }
            Forms.FolderBrowserDialog folderPaths = new Forms.FolderBrowserDialog();
            if (folderPaths.ShowDialog() == Forms.DialogResult.OK)
            {
                List<string> filePaths = new List<string>();
                if (folderPaths.SelectedPath == string.Empty)
                {
                    MessageBox.Show("没有选择任何文件夹", "提示");
                    return;
                }
                this.SearchUnderSpecifiedPolicy(
                    new string[] { folderPaths.SelectedPath }, filePaths);
                IEnumerable<ModelArg> modelArgs = filePaths.Select(s => new ModelArg(s));
                this.FilesDroppedToHash.AddRange(modelArgs);
                this.CreateTaskToEnqueueFilePaths(modelArgs);
            }
        }
    }
}
