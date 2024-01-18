using System.Windows;

namespace HashCalculator
{
    public partial class DoubleProgressWindow : Window
    {
        private readonly DoubleProgressModel viewModel;

        internal DoubleProgressWindow(DoubleProgressModel model)
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
}
