using System.Windows.Controls;

namespace HashCalculator
{
    public partial class SelectTargetsCmderCtrl : UserControl
    {
        internal SelectTargetsCmderCtrl(SelectTargetsCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
