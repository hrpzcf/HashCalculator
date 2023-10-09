using System.Windows.Controls;

namespace HashCalculator
{
    public partial class HashingTaskResultFilterCtrl : UserControl
    {
        internal HashingTaskResultFilterCtrl(HashingTaskResultFilter model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
