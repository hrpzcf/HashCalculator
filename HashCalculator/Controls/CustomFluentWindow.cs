using System;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HashCalculator;

/// <summary>
/// 自定义 <see cref="FluentWindow"/> 基类：集中处理主题背景色。
/// <para>
/// 背景：wpfui 的 <see cref="WindowBackgroundManager.UpdateBackground"/> 会通过
/// <see cref="WindowBackdrop.RemoveBackdrop"/> -&gt; RestoreContentBackground 用
/// <c>window.Resources[...]</c> 索引器查找 ApplicationBackgroundBrush，找不到时回退到硬编码的
/// #202020 / #FAFAFA，并直接赋给 <see cref="Window.Background"/>（不读取 Application.Resources，
/// 故 XAML 覆盖无效）。因此必须在此用 Dispatcher.BeginInvoke 延迟到 wpfui 强写背景之后再覆盖。
/// </para>
/// </summary>
public class CustomFluentWindow : FluentWindow
{
    public CustomFluentWindow()
    {
        // 窗口创建时应用一次当前主题背景色（新窗口弹出即生效，无需外部逐个调用）
        this.OnAppThemeChanged(ApplicationTheme.Unknown, Colors.Transparent);
        // 订阅主题变化，切换主题时自动重设所有窗口背景色
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
            return;
        }
        Action action = () =>
        {
            this.Background = new SolidColorBrush(theme == ApplicationTheme.Dark ?
                // 深色窗口背景 #FF2E2E2E，浅色窗口背景 #FFF3F3F3
                Color.FromArgb(0xFF, 0x28, 0x28, 0x28) : Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3));
        };
        // wpfui 在主题应用时（ApplicationThemeManager.Apply 内部）
        // 会强写 window.Background，此处延迟到其完成后执行，才能覆盖生效。
        this.Dispatcher.BeginInvoke(action);
    }
}
