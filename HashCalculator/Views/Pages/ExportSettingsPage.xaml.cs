using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Windows;

namespace HashCalculator.Views.Pages;

public partial class ExportSettingsPage : Page
{
    private readonly SettingsViewModel _viewModel = null;
    private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

    public ExportSettingsPage()
    {
        this._viewModel = Settings.Current;
        this.DataContext = this._viewModel;
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
            this._viewModel.SelectedTemplateForExport = textBox.DataContext as TemplateForExportModel;
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
