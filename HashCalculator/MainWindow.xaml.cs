using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static System.Environment;

namespace HashCalculator
{
    internal class CmpResBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            CmpRes cmpRes = (CmpRes)value;
            switch (cmpRes)
            {
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.NoResult:
                default:
                    return "Transparent";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
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
            // Win32 文件名不区分大小写
            string name = pair[1].Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            this.nameHashPairs[name] = hash;
            return true;
        }

        public CmpRes IsMatch(string hash, string name)
        {
            if (hash == null || name == null || this.nameHashPairs.Count == 0)
                return CmpRes.NoResult;
            hash = hash.Trim().ToUpper();
            // Win32 文件名不区分大小写
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
        private readonly ObservableCollection<ShaModel> models =
            new ObservableCollection<ShaModel>();
        private int DropedFileCount = 0;
        private readonly Queue<string> filepathsQueue = new Queue<string>();
        private readonly Action<Task> aupdate;
        private static readonly object locker = new object();
        private readonly NameHashPairs hashPairs = new NameHashPairs();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title += $" {Info.VERSION} — {Info.AUTHOR} @ www.52pojie.cn";
            this.uiDataGrid_Sha256Files.ItemsSource = this.models;
            this.aupdate = new Action<Task>(this.UpdateProgress);
            new Thread(this.ThreadAddShaModel) { IsBackground = true }.Start();
        }

        private void DataGrid_FilesToCalculate_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            Thread thread = new Thread(new ParameterizedThreadStart(this.EnqueueFilePaths))
            {
                IsBackground = true
            };
            thread.Start(data);
        }

        private void EnqueueFilePaths(object data)
        {
            string[] paths = data as string[];
            lock (locker)
            {
                foreach (string path in paths)
                {
                    this.filepathsQueue.Enqueue(path);
                }
                this.DropedFileCount += paths.Length;
                this.Dispatcher.Invoke(this.UpdateAfterFileTotalChanged);
            }
        }

        private void ShowProgressInfo()
        {
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
            this.uiButton_StartCompare.IsEnabled = true;
            this.uiButton_ClearModels.IsEnabled = true;
            this.uiButton_ExportAsText.IsEnabled = true;
            this.uiTextBlock_CompletedTotal.Visibility = Visibility.Hidden;
            this.uiTextBlock_CompletedCount.Visibility = Visibility.Hidden;
            this.uiTextBlock_Delimiter.Visibility = Visibility.Hidden;
            this.uiTextBlock_TotalTaskCount.Visibility = Visibility.Hidden;
            this.uiProgressbar_TaskProgress.Visibility = Visibility.Hidden;
        }

        private void UpdateAfterFileTotalChanged()
        {
            this.uiProgressbar_TaskProgress.Maximum = this.DropedFileCount;
            this.uiTextBlock_TotalTaskCount.Text = this.DropedFileCount.ToString();
            if (CompletionCounter.Count() <= 0)
                this.ShowProgressInfo();
        }

        private void AddShaModel(int serial, FileInfo fi)
        {
            ShaModel model = new ShaModel(serial, fi);
            this.models.Add(model);
            Task.Run(model.GenerateHashLimited).ContinueWith(this.aupdate);
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
            if (completed >= this.DropedFileCount)
            {
                CompletionCounter.ResetCount();
                this.DropedFileCount = 0;
                this.Dispatcher.Invoke(this.HideProgressInfo);
            }
        }

        private void ThreadAddShaModel()
        {
            int serial;
            FileInfo fi;
            Action<int, FileInfo> asm = new Action<int, FileInfo>(this.AddShaModel);
            while (true)
            {
                lock (locker)
                {
                    if (this.filepathsQueue.Count == 0)
                        goto pause;
                    fi = new FileInfo(this.filepathsQueue.Dequeue());
                    serial = SerialGenerator.GetSerial();
                }
                this.Dispatcher.Invoke(asm, serial, fi);
            pause:
                Thread.Sleep(10);
            }
        }

        // TODO 使用以下方法清空列表后，计算任务并未停下，需要增加停止计算任务的逻辑
        private void Button_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.models.Clear();
            SerialGenerator.Reset();
        }

        private void Button_ExportAsTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.models.Count == 0)
            {
                MessageBox.Show("列表中没有任何需要导出的条目。");
                return;
            }
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = "sha256sums.txt",
                InitialDirectory = GetFolderPath(SpecialFolder.Desktop),
            };
            if (sf.ShowDialog() != true)
            {
                return;
            }
            try
            {
                using (StreamWriter sw = File.CreateText(sf.FileName))
                {
                    foreach (ShaModel sm in this.models)
                    {
                        if (sm.Completed && sm.ToExport)
                        {
                            sw.WriteLine($"{sm.Sha256} *{sm.Name}");
                        }
                    }
                }
            }
            catch (Exception r)
            {
                MessageBox.Show($"结果导出为文本文件失败，原因：\n\t{r.Message}");
                return;
            }
        }

        private void GenerateHashPairs()
        {
            if (this.uiComboBox_ComparisonMethod.SelectedIndex == 0)
            {
                if (!File.Exists(this.uiTextBox_ValueToCompare.Text))
                {
                    MessageBox.Show("检验依据来源sha256sums文件不存在。");
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
                catch
                {
                    MessageBox.Show("检验依据来源sha256sums文件打开失败。");
                    return;
                }
            }
            else
            {
                this.hashPairs.Add(new string[] { this.uiTextBox_ValueToCompare.Text.Trim(), "" });
                return;
            }
        }

        private void Sha256Comparison()
        {
            this.hashPairs.Clear();
            if (string.IsNullOrEmpty(this.uiTextBox_ValueToCompare.Text))
            {
                MessageBox.Show("尚未输入哈希值检验依据。");
                return;
            }
            this.GenerateHashPairs();
            foreach (ShaModel sm in this.models)
                sm.CmpResult = this.hashPairs.IsMatch(sm.Sha256, sm.Name);
        }

        private void Button_StartCompare_Click(object sender, RoutedEventArgs e)
        {
            this.Sha256Comparison();
        }

        private void TextBox_ValueToCompare_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void TextBox_Sha256File_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
                return;
            this.uiTextBox_ValueToCompare.Text = data[0];
        }

        private void TextBox_TextContent_Changed(object sender, TextChangedEventArgs e)
        {
            if (File.Exists(this.uiTextBox_ValueToCompare.Text))
                this.uiComboBox_ComparisonMethod.SelectedIndex = 0;
            else
                this.uiComboBox_ComparisonMethod.SelectedIndex = 1;
        }
    }
}
