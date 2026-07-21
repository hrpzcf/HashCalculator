using HashCalculator.ViewModels.Windows;

namespace HashCalculator.Views.Windows;

public partial class ProgressWindow
{
    private readonly ProgressWindowModel viewModel;

    internal ProgressWindow(ProgressWindowModel model)
    {
        this.viewModel = model;
        this.DataContext = model;
        this.Closing += this.ChangeHashWindowClosing;
        this.InitializeComponent();
    }

    private void ChangeHashWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!this.viewModel.AutoClose)
        {
            e.Cancel = true;
        }
    }
}
