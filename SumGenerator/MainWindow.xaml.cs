using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using static System.Environment;

namespace SumGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ShaModel> models =
            new ObservableCollection<ShaModel>();
        private int TotalFileNum = 0;
        private readonly Queue<string> filepathsQueue = new Queue<string>();
        private readonly object locker = new object();

        public MainWindow()
        {
            this.InitializeComponent();
            this.uiDataGrid_Sha256Files.ItemsSource = this.models;
            Thread thread = new Thread(this.CalcSha256Thread) { IsBackground = true };
            thread.Start();
        }

        private void OnDataGrid_FilesToCheck_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] data) || data.Length == 0)
            {
                return;
            }
            Thread thread = new Thread(new ParameterizedThreadStart(this.EnqueueFilePaths))
            {
                IsBackground = true
            };
            thread.Start(data);
        }

        private void EnqueueFilePaths(object data)
        {
            string[] paths = data as string[];
            lock (this.locker)
            {
                foreach (string path in paths)
                {
                    this.filepathsQueue.Enqueue(path);
                }
                this.TotalFileNum += paths.Length;
                this.Dispatcher.Invoke(this.UpdateAfterFileTotalChanged);
            }
        }

        private void ShowProgressInfo()
        {
            this.uiButton_ClearQueue.IsEnabled = true;
            this.uiTextBlock_OpeningBracket.Visibility = Visibility.Visible;
            this.uiTextBlock_CheckedCount.Visibility = Visibility.Visible;
            this.uiTextBlock_Delimiter.Visibility = Visibility.Visible;
            this.uiTextBlock_TotalCount.Visibility = Visibility.Visible;
            this.uiTextBlock_ClosingBracket.Visibility = Visibility.Visible;
            this.uiTextBlock_CheckingFileName.Visibility = Visibility.Visible;
            this.uiProgressbar_TaskProgress.Visibility = Visibility.Visible;
        }

        private void HideProgressInfo()
        {
            this.uiButton_ClearQueue.IsEnabled = false;
            this.uiTextBlock_OpeningBracket.Visibility = Visibility.Hidden;
            this.uiTextBlock_CheckedCount.Visibility = Visibility.Hidden;
            this.uiTextBlock_Delimiter.Visibility = Visibility.Hidden;
            this.uiTextBlock_TotalCount.Visibility = Visibility.Hidden;
            this.uiTextBlock_ClosingBracket.Visibility = Visibility.Hidden;
            this.uiTextBlock_CheckingFileName.Visibility = Visibility.Hidden;
            this.uiProgressbar_TaskProgress.Visibility = Visibility.Hidden;
        }

        private void UpdateProgress(int nth, string name)
        {
            this.uiProgressbar_TaskProgress.Value = nth - 1;
            this.uiTextBlock_CheckedCount.Text = nth.ToString();
            this.uiTextBlock_CheckingFileName.Text = name;
        }

        private void UpdateAfterFileTotalChanged()
        {
            this.uiProgressbar_TaskProgress.Maximum = this.TotalFileNum;
            this.uiTextBlock_TotalCount.Text = this.TotalFileNum.ToString();
        }

        private void CalcSha256Thread()
        {
            bool firstStepIn = true;
            int serial;
            int checkedCount = 1;
            FileInfo fi;
            Action<ShaModel> ama = new Action<ShaModel>(this.models.Add);
            Action<int, string> aup = new Action<int, string>(this.UpdateProgress);
            while (true)
            {
                lock (this.locker)
                {
                    if (this.filepathsQueue.Count == 0)
                    {
                        goto pause;
                    }
                    fi = new FileInfo(this.filepathsQueue.Dequeue());
                    serial = SerialGenerator.GetSerial();
                }
                if (firstStepIn)
                {
                    firstStepIn = false;
                    this.Dispatcher.Invoke(this.ShowProgressInfo);
                }
                if (fi.Exists)
                {
                    this.Dispatcher.Invoke(aup, checkedCount, fi.Name);
                    this.Dispatcher.Invoke(ama, new ShaModel(serial, fi));
                }
                else
                {
                    SerialGenerator.SerialBack();
                }
                ++checkedCount; // 不管文件是否存在，都要自增文件计数
                lock (this.locker)
                {
                    if (this.filepathsQueue.Count == 0)
                    {
                        checkedCount = 1;
                        this.TotalFileNum = 0;
                        firstStepIn = true;
                        this.Dispatcher.Invoke(this.HideProgressInfo);
                    }
                }
            pause:
                Thread.Sleep(100);
            }
        }

        private void OnButton_ClearFileList_Click(object sender, RoutedEventArgs e)
        {
            this.models.Clear();
        }

        private void OnButton_ClearFileQueue_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_ClearQueue.IsEnabled = false;
            lock (this.locker)
            {
                this.filepathsQueue.Clear();
            }
        }

        private void OnButton_ExportAsTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.models.Count == 0)
            {
                MessageBox.Show("列表中没有任何需要导出的条目。");
                return;
            }
            SaveFileDialog sf = new SaveFileDialog()
            {
                ValidateNames = true,
                Filter = "文本文件|*.txt",
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
                        if (sm.ToExport)
                        {
                            sw.WriteLine($"{sm.Sha256} *{sm.Name}");
                        }
                    }
                }
            }
            catch (Exception r)
            {
                MessageBox.Show($"导出为文本文件失败，原因：\n\t{r.Message}");
                return;
            }
        }
    }
}
