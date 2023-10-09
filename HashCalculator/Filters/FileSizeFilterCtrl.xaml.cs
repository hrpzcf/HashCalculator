using System.Windows.Controls;

namespace HashCalculator
{
    public partial class FileSizeFilterCtrl : UserControl
    {
        internal FileSizeFilterCtrl(FileSizeFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
