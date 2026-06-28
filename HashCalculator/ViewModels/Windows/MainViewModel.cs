using System.Collections.ObjectModel;
using System.ComponentModel;
using HashCalculator.Views.Pages;
using Wpf.Ui.Controls;

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
        this.SetupNavigationViewItems();
    }

    public NavigationViewItem SettingsNavigationViewItem { get; private set; }

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
                this.starter.BeginAdjust(Settings.Current.SelectedTaskNumberLimit);
                break;
        }
    }

    private void SetupNavigationViewItems()
    {
        this.MenuItems = [new NavigationViewItem("计算哈希值", SymbolRegular.Home24, typeof(HomePage))];
        this.SettingsNavigationViewItem = new NavigationViewItem("设置", SymbolRegular.Settings24, typeof(SettingsPanel))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("常规设置", SymbolRegular.LauncherSettings20, typeof(GeneralSettingsPage)),
                new NavigationViewItem("界面设置", SymbolRegular.CalendarSettings20, typeof(InterfaceSettingsPage)),
                new NavigationViewItem("任务设置", SymbolRegular.ClipboardTextLtr20, typeof(TaskSettingsPage)),
                new NavigationViewItem("菜单与文件关联", SymbolRegular.CursorClick20, typeof(MenuSettingsPage)),
                new NavigationViewItem("算法别名设置", SymbolRegular.MathFormula20, typeof(AliasSettingsPage)),
                new NavigationViewItem("复制行为设置", SymbolRegular.ClipboardSettings20, typeof(CopySettingsPage)),
                new NavigationViewItem("导出行为设置", SymbolRegular.DocumentArrowUp20, typeof(ExportSettingsPage)),
                new NavigationViewItem("校验依据解析方案", SymbolRegular.DocumentBulletList20, typeof(ParsingSchemeSettingsPage)),
                new NavigationViewItem("快捷操作设置", SymbolRegular.Send20, typeof(ShortcutSettingsPage)),
                new NavigationViewItem("配置保存位置", SymbolRegular.Save20, typeof(ConfigSettingsPage)),
                new NavigationViewItem("关于软件", SymbolRegular.Info20, typeof(AboutSettingsPage)),
            }
        };
        this.FooterItems = [this.SettingsNavigationViewItem];
    }
}
