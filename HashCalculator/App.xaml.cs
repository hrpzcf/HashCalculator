using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 剪贴板无法打开(CLIPBRD_E_CANT_OPEN)错误代码：0x800401D0
            if ((uint)e.Exception.HResult == 0x800401D0)
                e.Handled = true;
            MessageBox.Show($"复制哈希值失败：\n{e.Exception.Message}", "错误");
        }
    }
}
