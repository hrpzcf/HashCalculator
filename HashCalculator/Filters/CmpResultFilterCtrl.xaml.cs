using System.Windows.Controls;

namespace HashCalculator
{
    public partial class CmpResultFilterCtrl : UserControl
    {
        private readonly AbsHashViewFilter _viewModel;

        internal CmpResultFilterCtrl(CmpResultFilter model)
        {
            this._viewModel = model;
            this.DataContext = this._viewModel;
            this.InitializeComponent();
        }

        private void ButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            this._viewModel.IsCaptionFlyoutShowed = !this._viewModel.IsCaptionFlyoutShowed;
        }
    }
}
