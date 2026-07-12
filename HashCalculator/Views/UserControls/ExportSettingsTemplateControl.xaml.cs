using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HashCalculator.Views.Windows;

namespace HashCalculator.Views.UserControls;

public partial class ExportSettingsTemplateControl : UserControl
{
    private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

    public ExportSettingsTemplateControl()
    {
        this.InitializeComponent();
    }

    private async void OnTextBoxExtensionLostFocus(object sender, RoutedEventArgs e)
    {
        int index;
        if (sender is TextBox textBox && (index = textBox.Text.IndexOfAny(invalidChars)) != -1)
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "警告", $"文件扩展名不能包含 <{textBox.Text[index]}> 字符，此方案将不起作用！");
            await Task.Delay(200);
            Settings.Current.SelectedTemplateForExport = textBox.DataContext as TemplateForExportModel;
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
