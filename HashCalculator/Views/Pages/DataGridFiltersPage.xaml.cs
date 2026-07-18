using System;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class DataGridFiltersPage : Page, INavigableView<FilterOperationModel>
{
    public FilterOperationModel ViewModel { get; }

    public DataGridFiltersPage(FilterOperationModel model)
    {
        this.ViewModel = model;
        this.DataContext = model;
        this.InitializeComponent();
    }

    // TODO: 改为 page unloaded 或者切走时执行都不行，需要改为按钮重置
    private void PanelClosed(object sender, EventArgs e)
    {
        this.ViewModel.ResetFiltersAndRefresh();
    }

    private void FiltersItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is AbsHashViewFilter filter)
        {
            this.ViewModel.SelectedFilter = filter;
        }
    }
}
