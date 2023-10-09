using System.Windows.Controls;

namespace HashCalculator
{
    public partial class CmpResultFilterCtrl : UserControl
    {
        internal CmpResultFilterCtrl(CmpResultFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
