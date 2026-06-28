using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class ParsingSchemeSettingsPage : Page
{
    public ParsingSchemeSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
