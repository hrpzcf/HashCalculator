using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HashCalculator;

/// <summary>
/// 自定义 <see cref="FluentWindow"/> 基类：集中处理主题背景色。
/// <para>
/// 背景：wpfui 的 <see cref="WindowBackgroundManager.UpdateBackground"/> 会通过
/// <see cref="WindowBackdrop.RemoveBackdrop"/> -&gt; RestoreContentBackground 用
/// <c>window.Resources[...]</c> 索引器查找 ApplicationBackgroundBrush（只查窗口自身 Resources，
/// 不读取 Application.Resources，故 XAML 覆盖无效），找不到时回退到硬编码的 #202020 / #FAFAFA，
/// 并直接赋给 <see cref="Window.Background"/>。
/// </para>
/// <para>
/// 解决方案：把主题背景画刷直接写入窗口自身 Resources 的 <c>ApplicationBackgroundBrush</c>，
/// 使 wpfui 强写背景时能直接命中我们的画刷，无需延迟覆盖、不闪烁，并随主题切换自动更新。
/// </para>
/// </summary>
public class CustomFluentWindow : FluentWindow
{
    private const string BackgroundBrushStr = "ApplicationBackgroundBrush";

    public CustomFluentWindow()
    {
        // 窗口创建时应用当前主题背景色，新窗口弹出即生效，无需外部逐个调用
        this.OnAppThemeChanged(ApplicationTheme.Unknown, Colors.Transparent);
        // 订阅主题变化，切换主题时自动更新所有窗口背景色
        ApplicationThemeManager.Changed += this.OnAppThemeChanged;
    }

    private void OnAppThemeChanged(ApplicationTheme theme, Color accent)
    {
        if (theme == ApplicationTheme.Unknown)
        {
            theme = ApplicationThemeManager.GetAppTheme();
        }
        // 高对比度不干预，窗口背景由系统控制
        if (theme != ApplicationTheme.Dark && theme != ApplicationTheme.Light)
        {
            this.Resources.Remove(BackgroundBrushStr);
            return;
        }
        // 写入窗口自身资源，wpfui 的 RestoreContentBackground 用 window.Resources[...]
        // 索引器查找 ApplicationBackgroundBrush 时会命中此处，从而直接用我们的画刷。
        this.Resources[BackgroundBrushStr] = new SolidColorBrush(theme == ApplicationTheme.Dark
            // 浅色窗口背景 #FF282828
            ? Color.FromArgb(0xFF, 0x28, 0x28, 0x28)
            // 浅色窗口背景 #FFF3F3F3
            : Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3));
    }
}
