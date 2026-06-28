using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class ConfigSettingsPage : Page
{
    public ConfigSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
