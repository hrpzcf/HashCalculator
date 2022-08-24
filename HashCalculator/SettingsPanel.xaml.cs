using System.Windows;

namespace HashCalculator
{
    /// <summary>
    /// SettingsPanel.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPanel : Window
    {
        public SettingsPanel()
        {
            this.InitializeComponent();
            this.InitializeFromConfigure(Settings.Current);
        }

        private void InitializeFromConfigure(Configure config)
        {
            this.uiCheckBox_RembMainSize.IsChecked = config.RembMainWinSize;
            this.uiComboBox_SearchPolicy.SelectedIndex = config.FolderSearchPolicy;
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_LoadDefault.IsEnabled = true;
            this.uiButton_Apply.IsEnabled = false;
            Settings.Current.RembMainWinSize = this.uiCheckBox_RembMainSize.IsChecked ?? false;
            Settings.Current.FolderSearchPolicy = this.uiComboBox_SearchPolicy.SelectedIndex;
            Settings.SaveConfigure();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_LoadDefault_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_LoadDefault.IsEnabled = false;
            this.uiButton_Apply.IsEnabled = true;
            this.InitializeFromConfigure(new Configure());
        }

        private void CheckBox_RembMainSize_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }
    }
}
