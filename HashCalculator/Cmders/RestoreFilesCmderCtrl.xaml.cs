using System.Windows.Controls;

namespace HashCalculator
{
    public partial class RestoreFilesCmderCtrl : UserControl
    {
        internal RestoreFilesCmderCtrl(RestoreFilesCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
