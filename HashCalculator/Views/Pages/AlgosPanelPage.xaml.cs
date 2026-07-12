using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class AlgosPanelPage : Page, INavigableView<AlgosPanelPageModel>
{
    public AlgosPanelPageModel ViewModel { get; }

    public AlgosPanelPage(AlgosPanelPageModel model)
    {
        this.ViewModel = model;
        this.DataContext = this.ViewModel;
        this.InitializeComponent();
    }
}
