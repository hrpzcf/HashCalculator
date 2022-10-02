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
            this.uiComboBox_SearchPolicy1.SelectionChanged += this.ComboBoxes_ActivateApplyButton_SelectionChanged;
            this.uiComboBox_SearchPolicy2.SelectionChanged += this.ComboBoxes_ActivateApplyButton_SelectionChanged;
            this.uiComboBox_SimulCalculate.SelectionChanged += this.ComboBoxes_ActivateApplyButton_SelectionChanged;
        }

        private void InitializeFromConfigure(Configure config)
        {
            this.uiCheckBox_RembMainSize.IsChecked = config.RembMainWindowSize;
            this.uiComboBox_SearchPolicy1.SelectedIndex = (int)config.DroppedSearchPolicy;
            this.uiCheckBox_UseLowercaseHash.IsChecked = config.UseLowercaseHash;
            this.uiCheckBox_RemMainWinPos.IsChecked = config.RemMainWindowPosition;
            this.uiComboBox_SimulCalculate.SelectedIndex = (int)config.SimulCalculate;
            this.uiComboBox_SearchPolicy2.SelectedIndex = (int)config.QuickVerificationSearchPolicy;
            this.Width = config.SettingsWinWidth;
            this.Height = config.SettingsWinHeight;
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = false;
            this.uiButton_LoadDefault.IsEnabled = true;
            Configure config = Settings.Current;
            config.RembMainWindowSize = this.uiCheckBox_RembMainSize.IsChecked ?? false;
            config.DroppedSearchPolicy = (SearchPolicy)this.uiComboBox_SearchPolicy1.SelectedIndex;
            config.UseLowercaseHash = this.uiCheckBox_UseLowercaseHash.IsChecked ?? false;
            config.RemMainWindowPosition = this.uiCheckBox_RemMainWinPos.IsChecked ?? false;
            config.SimulCalculate = (SimCalc)this.uiComboBox_SimulCalculate.SelectedIndex;
            config.QuickVerificationSearchPolicy = (SearchPolicy)this.uiComboBox_SearchPolicy2.SelectedIndex;
            //Settings.SaveConfigure(); // 窗口关闭时会 SaveConfigure
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

        private void Window_SettingsPanel_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Current.SettingsWinWidth = this.Width;
            Settings.Current.SettingsWinHeight = this.Height;
            Settings.SaveConfigure();
        }

        private void CheckBoxes_ActivateApplyButton_Click(object sender, RoutedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }

        private void ComboBoxes_ActivateApplyButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.uiButton_Apply.IsEnabled = true;
        }
    }
}
