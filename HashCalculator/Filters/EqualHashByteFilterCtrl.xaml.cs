using System.Windows.Controls;

namespace HashCalculator
{
    public partial class EqualHashByteFilterCtrl : UserControl
    {
        internal EqualHashByteFilterCtrl(EqualHashByteFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
