using System;
using System.Windows;
using System.Windows.Media;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.UserControls;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HashCalculator.Views.Windows;

public partial class FilterOperationWindow
{
    private NavigationService _navigationService = null;

    public FilterOperationWindow(
        MainWindow ownerWindow, FilterOperationModel model)
    {
        this.DataContext = model;
        this.InitializeComponent();
        this.InitializeNavigation(model);
        this.Owner = ownerWindow;
    }

    private void InitializeNavigation(FilterOperationModel model)
    {
        INavigationViewPageProvider pageProvider = App.GetRequiredService
            <INavigationViewPageProvider>();
        this._navigationService = new NavigationService(pageProvider);
        this._navigationService.SetNavigationControl(this.FilterOperationNavigationView);
        model.SetupModelNavigationService(this._navigationService);
    }

    private void OnThemeChanged(ApplicationTheme theme, Color accent)
    {
        if (!this.IsLoaded)
        {
            return;
        }
        WindowBackgroundManager.UpdateBackground(this, theme, WindowBackdropType.None);
    }

    private void FilterOperationWindowLoaded(object sender, RoutedEventArgs e)
    {
        //this.SetValue(BorderThicknessProperty, new Thickness(1));
        //this.SetValue(BorderBrushProperty, this.FindResource("SystemAccentColorBrush"));

        // 订阅主题变化事件：切换主题时手动刷新本窗口背景，
        // 因为 ApplicationThemeManager.Apply 只自动刷新主窗口背景，
        // 对非主窗口（本窗口）需要手动调用 WindowBackgroundManager.UpdateBackground。
        ApplicationThemeManager.Changed += this.OnThemeChanged;
        this._navigationService.Navigate(typeof(DataGridFiltersControl));
    }

    private void FilterOperationWindowClosed(object sender, EventArgs e)
    {
        ApplicationThemeManager.Changed -= this.OnThemeChanged;
        if (this.FilterOperationNavigationView.SelectedItem is NavigationViewItem navigationItem)
        {
            navigationItem.Deactivate(this.FilterOperationNavigationView);
        }
    }
}
