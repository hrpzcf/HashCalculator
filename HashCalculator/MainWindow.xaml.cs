using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HashCalculator
{
    internal class CmpResBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.NoResult:
                    return "#64888888";
                default:
                    return "Transparent";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class AlgoTypeBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((AlgoType)value)
            {
                case AlgoType.SHA256:
                    return "#640066FF";
                case AlgoType.SHA1:
                    return "#64FF0071";
                case AlgoType.SHA224:
                    return "#64331772";
                case AlgoType.SHA384:
                    return "#64FFBB33";
                case AlgoType.SHA512:
                    return "#64008B73";
                case AlgoType.MD5:
                    return "#64799B00";
                default:
                    return "#64FF0000";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AlgoType.SHA256; // 此处未使用，只返回默认值
        }
    }

    internal class NameHashPairs
    {
        private readonly Dictionary<string, string> nameHashPairs =
            new Dictionary<string, string>();

        public void Clear()
        {
            this.nameHashPairs.Clear();
        }

        public bool Add(string[] pair)
        {
            if (pair.Length < 2 || pair[0] == null || pair[1] == null)
                return false;
            string hash = pair[0].Trim().ToUpper();
            // Windows 文件名不区分大小写
            string name = pair[1].Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            this.nameHashPairs[name] = hash;
            return true;
        }

        public CmpRes IsMatch(string hash, string name)
        {
            if (hash == null || name == null || this.nameHashPairs.Count == 0)
                return CmpRes.NoResult;
            hash = hash.Trim().ToUpper();
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            if (this.nameHashPairs.Keys.Count == 1 && this.nameHashPairs.Keys.Contains(""))
                if (this.nameHashPairs.Values.Contains(hash))
                    return CmpRes.Matched;
                else
                    return CmpRes.NoResult;
            if (this.nameHashPairs.TryGetValue(name, out string dicHash))
                return dicHash == hash ? CmpRes.Matched : CmpRes.Mismatch;
            return CmpRes.NoResult;
        }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<HashModel> hashModels =
            new ObservableCollection<HashModel>();
        private int QueuedFilesCount = 0;
        private readonly Queue<string> filepathsQueue = new Queue<string>();
        private readonly Action<Task> aupdate;
        private readonly NameHashPairs hashPairs = new NameHashPairs();
        private readonly List<string> ComputedFilesPath = new List<string>();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = $"{Info.Title} v{Info.Version} — {Info.Author} @ {Info.Published}";
            this.uiDataGrid_HashFiles.ItemsSource = this.hashModels;
            this.aupdate = new Action<Task>(this.UpdateProgress);
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
            if (config.RembMainWinSize)
            {
                this.Width = config.MainWindowWidth;
                this.Height = config.MainWindowHeight;
            }
        }

        private void DataGrid_FilesToCalculate_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            this.ComputedFilesPath.AddRange(data);
            Thread thread = new Thread(new ParameterizedThreadStart(this.EnqueueFilesPath))
            {
                IsBackground = true
            };
            thread.Start(data);
        }

        private void EnqueueFilesPath(object data)
        {
            IEnumerable<string> paths = data as IEnumerable<string>;
            lock (Locks.MainLock)
            {
                foreach (string path in paths)
                    this.filepathsQueue.Enqueue(path);
                this.QueuedFilesCount += paths.Count();
                Application.Current.Dispatcher.Invoke(this.AfterFilesQueued);
            }
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
                this.ShowProgressInfo();
        }

        private void AddHashModel(int serial, FileInfo fi)
        {
            HashModel model = new HashModel(serial, fi);
            this.hashModels.Add(model);
            Task.Run(model.EnterGenerateUnderLimit).ContinueWith(this.aupdate);
        }

        private void UpdateProgress(Task task)
        {
            int completed = CompletionCounter.Count();
            this.Dispatcher.Invoke(
                () =>
                {
                    this.uiProgressbar_TaskProgress.Value = completed;
                    this.uiTextBlock_CompletedCount.Text = completed.ToString();
                }
            );
            if (completed >= this.QueuedFilesCount)
            {
                CompletionCounter.ResetCount();
                this.QueuedFilesCount = 0;
                this.Dispatcher.Invoke(this.HideProgressInfo);
            }
        }

        private void RefreshCurrentDataGridLines(bool deleteLines)
        {
            lock (Locks.MainLock)
            {
                if (deleteLines)
                {
                    this.hashModels.Clear();
                    SerialGenerator.Reset();
                }
                // 与 DataGrid_FilesToCalculate_Drop 方法类似
                Thread thread = new Thread(new ParameterizedThreadStart(this.EnqueueFilesPath))
                {
                    IsBackground = true
                };
                thread.Start(this.ComputedFilesPath);
            }
        }

        private void ThreadAddHashModel()
        {
            int serial;
            FileInfo fi;
            Action<int, FileInfo> asm = new Action<int, FileInfo>(this.AddHashModel);
            while (true)
            {
                lock (Locks.MainLock)
                {
                    if (this.filepathsQueue.Count == 0)
                        goto pause;
                    fi = new FileInfo(this.filepathsQueue.Dequeue());
                    serial = SerialGenerator.GetSerial();
                    Application.Current.Dispatcher.Invoke(asm, serial, fi);
                }
            pause:
                Thread.Sleep(10);
            }
        }

        // TODO 使用以下方法清空列表后，计算任务并未停下，需要增加停止计算任务的逻辑
        private void Button_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.hashModels.Clear();
            this.ComputedFilesPath.Clear();
            SerialGenerator.Reset();
        }

        private void Button_ExportAsTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.hashModels.Count == 0)
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
            Settings.SaveConfig();
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (HashModel hm in this.hashModels)
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

        private void GenerateHashPairs()
        {
            if (this.uiComboBox_ComparisonMethod.SelectedIndex == 0)
            {
                if (!File.Exists(this.uiTextBox_ValueToCompare.Text))
                {
                    MessageBox.Show("检验依据来源文件不存在。", "提示");
                    return;
                }
                try
                {
                    foreach (string line in File.ReadAllLines(this.uiTextBox_ValueToCompare.Text))
                        this.hashPairs.Add(
                            line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries)
                        );
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"检验依据来源文件打开失败：\n{ex.Message}", "错误");
                    return;
                }
            }
            else
                this.hashPairs.Add(new string[] { this.uiTextBox_ValueToCompare.Text.Trim(), "" });
        }

        private void Button_StartCompare_Click(object sender, RoutedEventArgs e)
        {
            this.hashPairs.Clear();
            if (string.IsNullOrEmpty(this.uiTextBox_ValueToCompare.Text))
            {
                MessageBox.Show("尚未输入哈希值检验依据。", "提示");
                return;
            }
            this.GenerateHashPairs();
            foreach (HashModel hm in this.hashModels)
                hm.CmpResult = this.hashPairs.IsMatch(hm.Hash, hm.Name);
        }

        private void TextBox_ValueToCompare_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBox_FilesToHash_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            this.uiTextBox_ValueToCompare.Text = data[0];
        }

        private void TextBox_TextContent_Changed(object sender, TextChangedEventArgs e)
        {
            this.uiComboBox_ComparisonMethod.SelectedIndex = File.Exists(
                this.uiTextBox_ValueToCompare.Text
            )
              ? 0
              : 1;
        }

        private void CheckBox_WindowTopmost_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = this.uiCheckBox_WindowTopMost.IsChecked == true;
            Settings.Current.MainWindowTopmost = this.Topmost;
            Settings.SaveConfig();
        }

        private void ComboBox_HashAlgorithm_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e
        )
        {
            lock (Locks.AlgoSelectionLock)
            {
                Settings.Current.SelectedAlgo =
                    (AlgoType)this.uiComboBox_HashAlgorithm.SelectedIndex;
                Settings.SaveConfig();
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

        private void Window_MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Settings.Current.RembMainWinSize)
            {
                Settings.Current.MainWindowWidth = this.Width;
                Settings.Current.MainWindowHeight = this.Height;
                Settings.SaveConfig();
            }
        }

        private void DataGrid_HashFiles_PrevKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.uiDataGrid_HashFiles.SelectedIndex = -1;
            }
        }
    }
}
