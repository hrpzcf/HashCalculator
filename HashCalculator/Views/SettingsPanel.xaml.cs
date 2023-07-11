using System.Windows;

namespace HashCalculator
{
    public partial class SettingsPanel : Window
    {
        public SettingsPanel()
        {
            Settings.Current.RunInMultiInstMode = MappedFiler.RunMultiMode;
            this.DataContext = Settings.Current;
            this.InitializeComponent();
        }
    }
}
