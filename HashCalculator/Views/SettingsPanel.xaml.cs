using System.Windows;

namespace HashCalculator
{
    public partial class SettingsPanel : Window
    {
        public SettingsPanel()
        {
            this.DataContext = Settings.Current;
            this.InitializeComponent();
        }
    }
}
