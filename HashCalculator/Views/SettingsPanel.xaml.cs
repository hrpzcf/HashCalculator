using System;
using System.IO;
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
                if (textBlock.Text == SettingsViewModel.FixAlgoDlls)
                {
                    if (Directory.Exists(Settings.libDir))
                    {
                        try
                        {
                            foreach (string path in Directory.GetFiles(
                                Settings.libDir, "*.dll", SearchOption.TopDirectoryOnly))
                            {
                                try
                                {
                                    File.Delete(path);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                    string message = Settings.ExtractEmbeddedAlgoDlls(force: true);
                    if (!string.IsNullOrEmpty(message))
                    {
                        MessageBox.Show(this, $"修复失败：\n{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(this, $"已经成功更新相关文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (textBlock.Text == SettingsViewModel.StringDllDir)
                {
                    CommonUtils.OpenFolderAndSelectItem(Settings.libDir);
                }
            }
        }
    }
}
