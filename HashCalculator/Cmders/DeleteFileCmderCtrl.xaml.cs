using System.Windows.Controls;

namespace HashCalculator
{
    public partial class DeleteFileCmderCtrl : UserControl
    {
        internal DeleteFileCmderCtrl(DeleteFileCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
