using System.Windows;
using System.Windows.Controls;

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
            this.uiComboBox_SearchPolicy.SelectionChanged += this.ComboBox_SelectionChanged;
            this.uiComboBox_SimulCalculate.SelectionChanged += this.ComboBox_SelectionChanged;
        }

        private void InitializeFromConfigure(Configure config)
        {
            this.uiCheckBox_RembMainSize.IsChecked = config.RembMainWindowSize;
            this.uiComboBox_SearchPolicy.SelectedIndex = config.FolderSearchPolicy;
            this.uiCheckBox_UseLowercaseHash.IsChecked = config.UseLowercaseHash;
            this.uiCheckBox_RemMainWinPos.IsChecked = config.RemMainWindowPosition;
            this.uiComboBox_SimulCalculate.SelectedIndex = (int)config.SimulCalculate;
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = false;
            this.uiButton_LoadDefault.IsEnabled = true;
            Configure config = Settings.Current;
            config.RembMainWindowSize = this.uiCheckBox_RembMainSize.IsChecked ?? false;
            config.FolderSearchPolicy = this.uiComboBox_SearchPolicy.SelectedIndex;
            config.UseLowercaseHash = this.uiCheckBox_UseLowercaseHash.IsChecked ?? false;
            config.RemMainWindowPosition = this.uiCheckBox_RemMainWinPos.IsChecked ?? false;
            config.SimulCalculate = (SimCalc)this.uiComboBox_SimulCalculate.SelectedIndex;
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }

        private void CheckBox_RemMainWinPos_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }

        private void CheckBox_UseLowercaseHash_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }
    }
}
