using System.Windows.Controls;

namespace HashCalculator
{
    public partial class MarkFilesCmderCtrl : UserControl
    {
        internal MarkFilesCmderCtrl(MarkFilesCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
