using System.Windows;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator
{
    public partial class AlgosPanel : Window
    {
        public AlgosPanel()
        {
            this.InitializeComponent();
            this.DataContext = new AlgosPanelPageModel();
        }

        private void OnButtonCloseWindowClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AlgosPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }
    }
}
