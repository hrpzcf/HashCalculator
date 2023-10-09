using System.Windows.Controls;

namespace HashCalculator
{
    public partial class FileIndexFilterCtrl : UserControl
    {
        internal FileIndexFilterCtrl(FileIndexFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
