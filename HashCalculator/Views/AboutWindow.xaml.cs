using System.Windows;

namespace HashCalculator
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            this.DataContext = new AboutWindowModel();
            this.InitializeComponent();
        }
    }
}
