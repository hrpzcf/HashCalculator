using System;
using System.Windows;
using System.Windows.Controls;
using HashCalculator.Views.Windows;

namespace HashCalculator.Views.Pages;

public partial class MenuSettingsPage : Page
{
    public MenuSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }

    private void ButtonUpdateApplicationPathClick(object sender, RoutedEventArgs e)
    {
        Exception exception;
        if ((exception = ShellExtHelper.RegUpdateAppPath()) == null)
        {
            NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "程序路径更新成功！");
        }
        else
        {
            NotificationSender.ShowMessageBox(MainWindow.Current, "错误", $"更新失败：{exception.Message}");
        }
    }

    private void ButtonBrowseInstallationLocationClick(object sender, RoutedEventArgs e)
    {
        CommonUtils.OpenFolderAndSelectItem(Settings.ConfigInfo.ShellExtensionDir);
    }
}
