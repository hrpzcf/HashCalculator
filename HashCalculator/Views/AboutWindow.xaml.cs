using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            this.InitializeComponent();
        }

        private void OpenLinkWithShellExecute(string link)
        {
            if (!string.IsNullOrEmpty(link))
            {
                NativeFunctions.ShellExecuteW(MainWindow.WndHandle, "open", link, null, null, ShowCmd.SW_NORMAL);
            }
        }

        private void OnAboutWndTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                if (textBlock.Text == "GitHub Issues")
                {
                    this.OpenLinkWithShellExecute("https://github.com/hrpzcf/HashCalculator/issues");
                }
                else if (textBlock.Text == "Gitee Issues")
                {
                    this.OpenLinkWithShellExecute("https://gitee.com/hrpzcf/HashCalculator/issues");
                }
                else if (textBlock.Text == "GitHub")
                {
                    this.OpenLinkWithShellExecute("https://github.com/hrpzcf/HashCalculator");
                }
                else if (textBlock.Text == "Gitee")
                {
                    this.OpenLinkWithShellExecute("https://gitee.com/hrpzcf/HashCalculator");
                }
                else if (textBlock.Text == "GitHub Wiki")
                {
                    this.OpenLinkWithShellExecute("https://github.com/hrpzcf/HashCalculator/wiki");
                }
                else if (textBlock.Text == "Gitee Wiki")
                {
                    this.OpenLinkWithShellExecute("https://gitee.com/hrpzcf/HashCalculator/wikis/Home");
                }
                else if (textBlock.Text == Info.Published)
                {
                    this.OpenLinkWithShellExecute(Info.Published);
                }
            }
        }
    }
}
