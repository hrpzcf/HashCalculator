using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    public partial class RenameFileCmderCtrl : UserControl
    {
        private static readonly char[] invalidFileNameChars =
            Path.GetInvalidFileNameChars();

        internal RenameFileCmderCtrl(RenameFileCmder model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }

        private void TextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.IndexOfAny(invalidFileNameChars) != -1)
            {
                e.Handled = true;
                NotificationSender.ShowMessageBox(
                    FilterAndCmdPanel.Current, "提示", $"这个字符(串)不能作为文件名：{e.Text}");
            }
        }
    }
}
