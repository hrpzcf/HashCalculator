using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator.Views.Pages;

public partial class DataGridOperationsPage : Page
{
    public DataGridOperationsPage(FilterOperationModel model)
    {
        this.DataContext = model;
        this.InitializeComponent();
    }
}
