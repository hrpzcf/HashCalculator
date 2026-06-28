using System.Windows.Controls;

namespace HashCalculator.Views.Pages;

public partial class TaskSettingsPage : Page
{
    public TaskSettingsPage()
    {
        this.DataContext = Settings.Current;
        this.InitializeComponent();
    }
}
