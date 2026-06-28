using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class AliasSettingsPage : Page
{
    public AliasSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
