using System.Windows.Controls;

namespace HashCalculator
{
    public partial class RenameFileCmderCtrl : UserControl
    {
        internal RenameFileCmderCtrl(RenameFileCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
