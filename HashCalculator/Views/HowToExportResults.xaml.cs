using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class HowToExportResults : Window
    {
        public HowToExportResults()
        {
            this.DataContext = Settings.Current;
            this.InitializeComponent();
        }

        private void ButtonConfirmClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
