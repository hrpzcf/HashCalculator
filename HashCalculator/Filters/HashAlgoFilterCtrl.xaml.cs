using System.Windows.Controls;

namespace HashCalculator
{
    public partial class HashAlgoFilterCtrl : UserControl
    {
        internal HashAlgoFilterCtrl(HashAlgoFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
