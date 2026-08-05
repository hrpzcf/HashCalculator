using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator.Views.Pages;

public partial class DataGridFiltersPage : Page
{
    public DataGridFiltersPage(FilterOperationModel model)
    {
        this.DataContext = model;
        this.InitializeComponent();
    }
}
