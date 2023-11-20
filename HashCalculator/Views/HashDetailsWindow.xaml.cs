using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class HashDetailsWnd : Window
    {
        public static HashDetailsWnd This { get; private set; }

        internal HashDetailsWnd(HashViewModel model)
        {
            This = this;
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

        private void OnHashResultDataGridLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid hashTable)
            {
                hashTable.SelectedItem = null;
            }
        }
    }
}
