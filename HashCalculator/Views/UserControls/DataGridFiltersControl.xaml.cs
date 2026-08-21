using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator.Views.UserControls;

public partial class DataGridFiltersControl : UserControl
{
    private readonly FilterOperationModel _viewModel;

    public DataGridFiltersControl(FilterOperationModel model)
    {
        this._viewModel = model;
        this.DataContext = this._viewModel;
        this.InitializeComponent();
    }

    private void FiltersItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is AbsHashViewFilter filter)
        {
            this._viewModel.SelectedFilter = filter;
        }
    }
}
