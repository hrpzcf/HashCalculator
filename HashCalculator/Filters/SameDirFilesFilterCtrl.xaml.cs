using System.Windows.Controls;

namespace HashCalculator
{
    public partial class SameDirFilesFilterCtrl : UserControl
    {
        internal SameDirFilesFilterCtrl(SameDirFilesFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
