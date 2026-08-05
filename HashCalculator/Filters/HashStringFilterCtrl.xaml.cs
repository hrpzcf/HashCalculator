using System.Windows.Controls;

namespace HashCalculator
{
    public partial class HashStringFilterCtrl : UserControl
    {
        private readonly AbsHashViewFilter _viewModel;

        internal HashStringFilterCtrl(HashStringFilter model)
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
