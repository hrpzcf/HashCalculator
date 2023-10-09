using System.Windows.Controls;

namespace HashCalculator
{
    public partial class FileNameFilterCtrl : UserControl
    {
        internal FileNameFilterCtrl(FileNameFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
