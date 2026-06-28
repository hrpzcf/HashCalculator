using System.Windows.Controls;
using HashCalculator.ViewModels.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class SettingsPanelPage : Page, INavigableView<MainViewModel>
{
    public MainViewModel ViewModel { get; init; }

    public SettingsPanelPage(MainViewModel viewModel)
    {
        this.ViewModel = viewModel;
        this.DataContext = viewModel;
        Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
        this.InitializeComponent();
    }
}
