using System.Windows;

namespace HashCalculator.Controls;

/// <summary>
/// 继承 wpfui 的 CardControl，为内容区（Content 所在的 ContentPresenter）增加
/// 最小宽度/高度限制。默认 0，即不限制；设置后内容区所在列会被撑到该最小尺寸。
/// </summary>
public class MinLimitedCardControl : Wpf.Ui.Controls.CardControl
{
    /// <summary>标识 <see cref="MinContentWidth"/> 依赖属性。</summary>
    public static readonly DependencyProperty MinContentWidthProperty = DependencyProperty.Register(
        nameof(MinContentWidth),
        typeof(double),
        typeof(MinLimitedCardControl),
        new PropertyMetadata(0d));

    /// <summary>标识 <see cref="MinContentHeight"/> 依赖属性。</summary>
    public static readonly DependencyProperty MinContentHeightProperty = DependencyProperty.Register(
        nameof(MinContentHeight),
        typeof(double),
        typeof(MinLimitedCardControl),
        new PropertyMetadata(0d));

    /// <summary>
    /// 获取或设置内容区的最小宽度。默认 0，即不限制。
    /// </summary>
    public double MinContentWidth
    {
        get => (double)this.GetValue(MinContentWidthProperty);
        set => this.SetValue(MinContentWidthProperty, value);
    }

    /// <summary>
    /// 获取或设置内容区的最小高度。默认 0，即不限制。
    /// </summary>
    public double MinContentHeight
    {
        get => (double)this.GetValue(MinContentHeightProperty);
        set => this.SetValue(MinContentHeightProperty, value);
    }
}
