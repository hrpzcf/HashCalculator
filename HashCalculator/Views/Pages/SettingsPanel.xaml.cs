using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class SettingsPanel : Page, INavigableView<SettingsViewModel>
{
    private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

    public SettingsViewModel ViewModel { get; }

    public static SettingsPanel Current { get; private set; }

    public SettingsPanel()
    {
        Current = this;
        this.ViewModel = Settings.Current;
        this.DataContext = this.ViewModel;
        Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
        this.InitializeComponent();
    }

    private void SettingsPanelKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape &&
            sender is DataGrid && this.ViewModel.SelectedTemplateForExport != null)
        {
            e.Handled = true;
            this.ViewModel.SelectedTemplateForExport = null;
        }
    }

    private void ResetMainWindowDataGridColumnsIndex(object sender, RoutedEventArgs e)
    {
        int index = 0;
        foreach (DataGridColumn column in HomePage.Current.MainDataGrid.Columns)
        {
            column.DisplayIndex = index++;
        }
    }

    private void ResetMainWindowDataGridColumnsWidth(object sender, RoutedEventArgs e)
    {
        foreach (DataGridColumn column in HomePage.Current.MainDataGrid.Columns)
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
                    NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "程序路径更新成功！");
                }
                else
                {
                    NotificationSender.ShowMessageBox(MainWindow.Current, "错误", $"更新失败：{exception.Message}");
                }
            }
        }
    }

    private async void OnTextBoxExtensionLostFocus(object sender, RoutedEventArgs e)
    {
        int index;
        if (sender is TextBox textBox && (index = textBox.Text.IndexOfAny(invalidChars)) != -1)
        {
            NotificationSender.ShowMessageBox(MainWindow.Current, "警告",
                $"文件扩展名不能包含 <{textBox.Text[index]}> 字符，此方案将不起作用！");
            await Task.Delay(200);
            this.ViewModel.SelectedTemplateForExport = textBox.DataContext as TemplateForExportModel;
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
