using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class SettingsPanel : Window
    {
        private readonly SettingsViewModel viewModel;
        private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

        public static SettingsPanel Current { get; private set; }

        public SettingsPanel()
        {
            Current = this;
            this.viewModel = Settings.Current;
            this.DataContext = this.viewModel;
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            this.InitializeComponent();
        }

        private void SettingsPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (sender is Window)
                {
                    this.Close();
                    e.Handled = true;
                }
                else if (sender is DataGrid && this.viewModel.SelectedTemplateForExport != null)
                {
                    this.viewModel.SelectedTemplateForExport = null;
                    e.Handled = true;
                }
            }
        }

        private void SettingsPanelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = this.viewModel.ProcessingShellExtension;
        }

        private void ResetMainWindowDataGridColumnsIndex(object sender, RoutedEventArgs e)
        {
            int index = 0;
            foreach (DataGridColumn column in MainWindow.Current.MainWindowDataGrid.Columns)
            {
                column.DisplayIndex = index++;
            }
        }

        private void ResetMainWindowDataGridColumnsWidth(object sender, RoutedEventArgs e)
        {
            foreach (DataGridColumn column in MainWindow.Current.MainWindowDataGrid.Columns)
            {
                column.Width = DataGridLength.Auto;
            }
        }

        private void OnTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                if (textBlock.Text == SettingsViewModel.ShellExtDir)
                {
                    CommonUtils.OpenFolderAndSelectItem(Settings.ConfigInfo.ShellExtensionDir);
                }
                else if (textBlock.Text == SettingsViewModel.UpdateExePath)
                {
                    Exception exception = ShellExtHelper.RegUpdateAppPath();
                    if (exception == null)
                    {
                        NotificationSender.ShowMessageBox(this, "提示", "程序路径更新成功！");
                    }
                    else
                    {
                        NotificationSender.ShowMessageBox(this, "错误", $"更新失败：{exception.Message}");
                    }
                }
            }
        }

        private async void OnTextBoxExtensionLostFocus(object sender, RoutedEventArgs e)
        {
            int index;
            if (sender is TextBox textBox && (index = textBox.Text.IndexOfAny(invalidChars)) != -1)
            {
                NotificationSender.ShowMessageBox(this, "警告", 
                    $"文件扩展名不能包含 <{textBox.Text[index]}> 字符，此方案将不起作用！");
                await Task.Delay(200);
                this.viewModel.SelectedTemplateForExport = textBox.DataContext as TemplateForExportModel;
                textBox.Focus();
                textBox.SelectAll();
            }
        }
    }
}
