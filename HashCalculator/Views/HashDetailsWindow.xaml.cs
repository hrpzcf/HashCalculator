using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class HashDetailsWnd : Window
    {
        internal HashDetailsWnd(HashViewModel model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }

        private void HashDetailsWndKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void OnHashDetailsLostFocus(object sender, RoutedEventArgs e)
        {
            this.hashDetails.SelectedItem = null;
        }
    }
}
