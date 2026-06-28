using System.Windows;
using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class InterfaceSettingsPage : Page
{
    public InterfaceSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }

    private void ResetHomePageDataGridColumnsIndex(object sender, RoutedEventArgs e)
    {
        int index = 0;
        foreach (DataGridColumn column in HomePage.Current.MainDataGrid.Columns)
        {
            column.DisplayIndex = index++;
        }
    }

    private void ResetHomePageDataGridColumnsWidth(object sender, RoutedEventArgs e)
    {
        foreach (DataGridColumn column in HomePage.Current.MainDataGrid.Columns)
        {
            column.Width = DataGridLength.Auto;
        }
    }
}
