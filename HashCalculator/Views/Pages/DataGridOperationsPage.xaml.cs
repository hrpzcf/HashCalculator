using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class DataGridOperationsPage : Page, INavigableView<FilterOperationModel>
{
    public FilterOperationModel ViewModel { get; }

    public DataGridOperationsPage(FilterOperationModel model)
    {
        this.ViewModel = model;
        this.DataContext = model;
        this.InitializeComponent();
    }
}
