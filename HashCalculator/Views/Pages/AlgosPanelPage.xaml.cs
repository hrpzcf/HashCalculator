using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HashCalculator.Views.Pages;

public partial class AlgosPanelPage : Page, INavigableView<AlgorithmsModel>
{
    public AlgorithmsModel ViewModel { get; }

    public AlgosPanelPage(AlgorithmsModel model)
    {
        this.ViewModel = model;
        this.DataContext = this.ViewModel;
        this.InitializeComponent();
    }
}
