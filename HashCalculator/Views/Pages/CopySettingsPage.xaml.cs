using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class CopySettingsPage : Page
{
    public CopySettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
