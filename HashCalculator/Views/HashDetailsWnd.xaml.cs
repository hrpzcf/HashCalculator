using System.Windows;

namespace HashCalculator
{
    public partial class HashDetailsWnd : Window
    {
        internal HashDetailsWnd(HashViewModel model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
