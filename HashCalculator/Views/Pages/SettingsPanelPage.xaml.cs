using System.Windows.Controls;
using HashCalculator.ViewModels.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class SettingsPanelPage : Page, INavigableView<MainWindowModel>
{
    public MainWindowModel ViewModel { get; init; }

    public SettingsPanelPage(MainWindowModel viewModel)
    {
        this.ViewModel = viewModel;
        this.DataContext = viewModel;
        this.InitializeComponent();
    }
}
