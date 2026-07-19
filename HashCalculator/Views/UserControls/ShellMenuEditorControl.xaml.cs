using System.Windows.Controls;
using HashCalculator.ViewModels.UserControls;

namespace HashCalculator.Views.UserControls;

public partial class ShellMenuEditorControl : UserControl
{
    public ShellMenuEditorControl()
    {
        this.DataContext = new ShellMenuEditorModel();
        this.InitializeComponent();
    }
}
