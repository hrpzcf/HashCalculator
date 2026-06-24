using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;

namespace HashCalculator.Views.Pages;

public partial class HomePage : Page
{
    private readonly HomeViewModel viewModel = null;

    public HomePage(HomeViewModel viewModel)
    {
        this.viewModel = viewModel;
        this.DataContext = this.viewModel;
        this.InitializeComponent();
    }

    private void DataGridHashingFilesDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is string[] data && data.Length != 0)
        {
            // 当用户把本地磁盘分区图标拖入程序窗口
            if (data[0].EndsWith(":\\"))
            {
                // 只要确定第一个是分区根目录，那其他项也都是分区根目录
                // 因为 Windows 不支持把不同区域的内容同时拖入程序窗口
                this.viewModel.BeginDisplayModels(data.Select(
                    partition => new PathPackage(partition, partition,
                    Settings.Current.SelectedSearchMethodForDragDrop)).ToArray());
            }
            else
            {
                string parentDir = Path.GetDirectoryName(data[0]);
                this.viewModel.BeginDisplayModels(new PathPackage(parentDir, data,
                    Settings.Current.SelectedSearchMethodForDragDrop));
            }
        }
    }

    private void DataGridHashingFilesPrevKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && sender is DataGrid dataGrid)
        {
            dataGrid.SelectedItem = null;
        }
    }

    private void TextBoxHashStringOrChecklistPathPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void TextBoxHashOrFilePathPreviewDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetData(DataFormats.FileDrop) is not string[] data || data.Length == 0)
        {
            return;
        }
        this.viewModel.HashStringOrChecklistPath = data[0];
    }
}
