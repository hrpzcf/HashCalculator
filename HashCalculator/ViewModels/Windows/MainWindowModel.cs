using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace HashCalculator.ViewModels.Windows;

public class MainWindowModel : BaseViewModel
{
    private RelayCommand navigatenFromSettingsPanelCmd;

    public MainWindowModel(INavigationService navigationService)
    {
        this.NavigationService = navigationService;
        Settings.Current.PropertyChanged += this.OnSettingsPropChanged;
        this.SetupNavigationViewItems();
    }

    public INavigationService NavigationService { get; private set; }

    public NavigationViewItem SettingsNavigationItem { get; private set; }

    public object[] SettingsNavigationItemsSource { get; private set; }

    public ObservableCollection<NavigationViewItem> MenuItems { get; private set; }

    public ObservableCollection<NavigationViewItem> FooterItems { get; private set; }

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
                HomeViewModel.Current.Starter.BeginAdjust(Settings.Current.SelectedTaskNumberLimit);
                break;
        }
    }

    private void SetupNavigationViewItems()
    {
        this.SettingsNavigationItem = new NavigationViewItem(
            "设置", SymbolRegular.Settings24, typeof(SettingsPanelPage));
        this.SettingsNavigationItemsSource =
        [
            new NavigationViewItem(
                "常规设置", SymbolRegular.LauncherSettings20, typeof(GeneralSettingsPage))
            {
                TargetPageTag = "常用设置和其他不便分类的设置项。"
            },
            new NavigationViewItem(
                "界面设置", SymbolRegular.CalendarSettings20, typeof(InterfaceSettingsPage))
            {
                TargetPageTag = "应用主题、部分控件的显示和隐藏等设置项。"
            },
            new NavigationViewItem(
                "任务设置", SymbolRegular.ClipboardTextLtr20, typeof(TaskSettingsPage))
            {
                TargetPageTag = "与哈希值计算任务有关的设置项。"
            },
            new NavigationViewItem(
                "菜单与文件关联", SymbolRegular.CursorClick20, typeof(MenuSettingsPage))
            {
                TargetPageTag = "安装或卸载系统右键菜单扩展和关联 HCB 格式校验依据。"
            },
            new NavigationViewItem(
                "算法别名", SymbolRegular.MathFormula20, typeof(AliasSettingsPage))
            {
                TargetPageTag = "给算法设置别名以识别校验依据中写法不一样的算法名。"
            },
            new NavigationViewItem(
                "复制行为", SymbolRegular.ClipboardSettings20, typeof(CopySettingsPage))
            {
                TargetPageTag = "自定义复制模板，用于控制复制出来的文本排版格式。"
            },
            new NavigationViewItem(
                "导出行为", SymbolRegular.DocumentArrowUp20, typeof(ExportSettingsPage))
            {
                TargetPageTag = "将计算结果导出到文本文件时的模板和导出行为的设置项。"
            },
            new NavigationViewItem(
                "校验依据解析方案", SymbolRegular.DocumentBulletList20, typeof(ParsingSchemeSettingsPage))
            {
                TargetPageTag = "编辑或自定义从校验依据文本/文件解析出校验信息的解析方案。"
            },
            new NavigationViewItem(
                "快捷操作", SymbolRegular.Send20, typeof(ShortcutSettingsPage))
            {
                TargetPageTag = "部分列的鼠标左键双击响应行为设置项。"
            },
            new NavigationViewItem(
                "配置保存位置", SymbolRegular.Save20, typeof(ConfigSettingsPage))
            {
                TargetPageTag = "控制本程序的设置文件保存位置的设置项。"
            },
            new NavigationViewItem("关于软件", SymbolRegular.Info20, typeof(AboutSettingsPage))
            {
                TargetPageTag = "本程序的项目信息、链接、使用的开源项目等。"
            }
        ];
        this.SettingsNavigationItem.MenuItemsSource = this.SettingsNavigationItemsSource;
        this.MenuItems = [
            new NavigationViewItem("主页", SymbolRegular.Home24, typeof(HomePage)),
            new NavigationViewItem("算法", SymbolRegular.MathFormula16, typeof(AlgosPanelPage)),
            new NavigationViewItem("筛选", SymbolRegular.Filter12, typeof(DataGridFiltersPage)),
            new NavigationViewItem("执行", SymbolRegular.DesktopEdit20, typeof(DataGridOperationsPage)),
        ];
        this.FooterItems = [this.SettingsNavigationItem];
    }

    private void NavigatenFromSettingsPanelAction(object param)
    {
        if (param is Type type)
        {
            this.NavigationService.Navigate(type);
        }
    }

    public ICommand NavigatenFromSettingsPanelCmd
    {
        get
        {
            this.navigatenFromSettingsPanelCmd ??= new RelayCommand(this.NavigatenFromSettingsPanelAction);
            return this.navigatenFromSettingsPanelCmd;
        }
    }
}
