using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class SettingsPanel : Page, INavigableView<SettingsViewModel>
{
    public SettingsViewModel ViewModel { get; }

    public static SettingsPanel Current { get; private set; }

    public SettingsPanel()
    {
        Current = this;
        this.ViewModel = Settings.Current;
        this.DataContext = this.ViewModel;
        Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
        this.InitializeComponent();
    }
}
