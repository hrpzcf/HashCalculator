using System;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Windows;

namespace HashCalculator.Views.Pages;

public partial class MenuSettingsPage : Page
{
    public MenuSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
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
}
