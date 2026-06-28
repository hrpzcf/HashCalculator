using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class AboutSettingsPage : Page
{
    public AboutSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
