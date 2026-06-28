using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class ShortcutSettingsPage : Page
{
    public ShortcutSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
