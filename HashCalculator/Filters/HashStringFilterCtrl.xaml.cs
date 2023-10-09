using System.Windows.Controls;

namespace HashCalculator
{
    public partial class HashStringFilterCtrl : UserControl
    {
        internal HashStringFilterCtrl(HashStringFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
