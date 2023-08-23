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

        private void HashDetailsWndKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }
    }
}
