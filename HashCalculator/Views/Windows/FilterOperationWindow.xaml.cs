using System.Windows;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.UserControls;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
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
        INavigationViewPageProvider pageProvider =
            App.GetRequiredService<INavigationViewPageProvider>();
        this._navigationService = new NavigationService(pageProvider);
        this._navigationService.SetNavigationControl(this.NavigationViewOnFilterOperationWindow);
        model.SetupModelNavigationService(this._navigationService);
    }

    private void FilterOperationWindowLoaded(object sender, RoutedEventArgs e)
    {
        //this.SetValue(BorderThicknessProperty, new Thickness(1));
        //this.SetValue(BorderBrushProperty, this.FindResource("SystemAccentColorBrush"));

        this._navigationService.Navigate(typeof(DataGridFiltersControl));
    }

    private void FilterOperationWindowClosed(object sender, System.EventArgs e)
    {
        if (this.NavigationViewOnFilterOperationWindow.SelectedItem is NavigationViewItem item)
        {
            item.Deactivate(this.NavigationViewOnFilterOperationWindow);
        }
    }
}
