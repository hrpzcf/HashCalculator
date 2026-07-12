using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class ExportSettingsPage : Page
{
    public ExportSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
