using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator.Views.UserControls;

public partial class DataGridOperationsControl : UserControl
{
    public DataGridOperationsControl(FilterOperationModel model)
    {
        this.DataContext = model;
        this.InitializeComponent();
    }
}
