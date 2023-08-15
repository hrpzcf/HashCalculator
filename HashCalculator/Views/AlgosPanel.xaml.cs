using System.Windows;

namespace HashCalculator
{
    public partial class AlgosPanel : Window
    {
        public AlgosPanel()
        {
            this.InitializeComponent();
            this.DataContext = new AlgosPanelModel();
        }

        private void OnButtonCloseWindowClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
