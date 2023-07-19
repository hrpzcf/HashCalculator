using System.Windows;

namespace HashCalculator
{
    public partial class SettingsPanel : Window
    {
        private readonly SettingsViewModel viewModel;

        public static SettingsPanel This { get; private set; }

        public SettingsPanel()
        {
            this.viewModel = Settings.Current;
            this.DataContext = Settings.Current;
            Settings.Current.RunInMultiInstMode = MappedFiler.RunMultiMode;
            this.InitializeComponent();
            This = this;
        }

        private void SettingsPanelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !this.viewModel.NotSettingShellExtension;
        }
    }
}
