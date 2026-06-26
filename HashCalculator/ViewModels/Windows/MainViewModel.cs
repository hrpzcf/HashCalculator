using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using HashCalculator.Others;
using HashCalculator.Views.Pages;
using HashCalculator.Views.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wpf.Ui.Controls;
using WpfuiCtrls = Wpf.Ui.Controls;

namespace HashCalculator.ViewModels.Windows;

public class MainViewModel : BaseViewModel
{
    private readonly ModelStarter starter = new ModelStarter(
        Settings.Current.SelectedTaskNumberLimit, 32);

    public static MainViewModel Current { get; private set; }

    public MainViewModel()
    {
        Current = this;
        Settings.Current.PropertyChanged += this.OnSettingsPropChanged;
    }

    public ObservableCollection<NavigationViewItem> MenuItems { get; } = 
        [
            new NavigationViewItem("计算哈希值", SymbolRegular.Home24, typeof(HomePage)),
        ];

    public ObservableCollection<NavigationViewItem> FooterItems { get; } =
        [
            new NavigationViewItem("设置", SymbolRegular.Settings24, typeof(SettingsPanel)),
        ];

    /// <summary>
    /// 需要立即响应的设置变更
    /// </summary>
    private void OnSettingsPropChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Current.RunInMultiInstMode):
                Initializer.RunMultiMode = Settings.Current.RunInMultiInstMode;
                break;
            case nameof(Settings.Current.SelectedTaskNumberLimit):
                this.starter.BeginAdjust(Settings.Current.SelectedTaskNumberLimit);
                break;
        }
    }
}
