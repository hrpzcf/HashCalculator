using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class GeneralSettingsPage : Page
{
    public GeneralSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
