using System;
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

    // TODO: 需要移到窗口的 cs 代码逻辑里
    private void PanelClosed(object sender, EventArgs e)
    {
        this._viewModel.ResetFiltersAndRefresh();
    }

    private void FiltersItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is AbsHashViewFilter filter)
        {
            this._viewModel.SelectedFilter = filter;
        }
    }
}
