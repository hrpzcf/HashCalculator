using System.Windows.Controls;

namespace HashCalculator
{
    public partial class SerialNumberFilterCtrl : UserControl
    {
        internal SerialNumberFilterCtrl(SerialNumberFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
