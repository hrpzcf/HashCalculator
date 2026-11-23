using System.Windows;
using System.Windows.Controls;

namespace HashCalculator.Views.UserControls;

public partial class BusyOverlay : UserControl
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(BusyOverlay),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(BusyOverlay),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ContentOffsetXProperty =
        DependencyProperty.Register(nameof(ContentOffsetX), typeof(double), typeof(BusyOverlay),
            new PropertyMetadata(0D));

    public BusyOverlay()
    {
        this.InitializeComponent();
    }

    public bool IsActive
    {
        get => (bool)this.GetValue(IsActiveProperty);
        set => this.SetValue(IsActiveProperty, value);
    }

    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>
    /// 遮罩中心内容的水平偏移量。<br/>
    /// 遮罩所在的页面右侧有导航栏时，页面中心相对窗口中心偏右，
    /// 用负值把中心内容左移回窗口的视觉中心。
    /// </summary>
    public double ContentOffsetX
    {
        get => (double)this.GetValue(ContentOffsetXProperty);
        set => this.SetValue(ContentOffsetXProperty, value);
    }
}
