using System.Windows;
using System.Windows.Controls;

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

        private void SettingsPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }

        private void SettingsPanelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !this.viewModel.NotSettingShellExtension;
        }

        private void OnTextBlockMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                bool showMessage = true;
                string message = string.Empty;
                if (textBlock.Text == SettingsViewModel.FixBlake2)
                {
                    message = Settings.ExtractBlake2Dll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixBlake3)
                {
                    message = Settings.ExtractBlake3Dll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixQuickXor)
                {
                    message = Settings.ExtractQuickXorDll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixSha224)
                {
                    message = Settings.ExtractSha2Dll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixSha3)
                {
                    message = Settings.ExtractKeccakDll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixStreebog)
                {
                    message = Settings.ExtractStreebogDll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixWhirlpool)
                {
                    message = Settings.ExtractWhirlpoolDll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixXxHash)
                {
                    message = Settings.ExtractXxHashDll(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.FixAlgoDlls)
                {
                    message = Settings.ExtractEmbeddedAlgoDlls(force: true);
                }
                else if (textBlock.Text == SettingsViewModel.StringDllDir)
                {
                    showMessage = false;
                    CommonUtils.OpenFolderAndSelectItem(Settings.libDir);
                }
                if (showMessage)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        MessageBox.Show(this, $"修复失败：\n{message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(this, $"已经成功更新相关文件", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
