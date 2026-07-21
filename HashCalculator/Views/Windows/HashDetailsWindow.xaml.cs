using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator.Views.Windows;

public partial class HashDetailsWindow
{
    internal HashDetailsWindow(HashViewModel model)
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

    private void OnHashResultDataGridLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid hashTable)
        {
            hashTable.SelectedItem = null;
        }
    }
}
