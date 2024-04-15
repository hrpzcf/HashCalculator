using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace HashCalculator
{
    public partial class FilterAndCmdPanel : Window
    {
        private WindowInteropHelper _interopHelper;
        private readonly FilterAndCmdPanelModel model;

        public static FilterAndCmdPanel This { get; private set; }

        public FilterAndCmdPanel(EventHandler handler)
        {
            this.model = new FilterAndCmdPanelModel();
            this.DataContext = this.model;
            This = this;
            this.Closed += handler;
            this.Closed += this.PanelClosed;
            this.Loaded += this.FilterAndCmderWndLoaded;
            this.InitializeComponent();
        }

        private void PanelClosed(object sender, EventArgs e)
        {
            this.model.ResetFiltersAndRefresh();
        }

        private void FilterAndCmderWndLoaded(object sender, RoutedEventArgs e)
        {
            this._interopHelper = new WindowInteropHelper(this);
            this.CheckPanelPosition();
        }

        public bool CheckPanelPosition()
        {
            bool windowStateChanged = false;
            if (this.Visibility != Visibility.Visible)
            {
                this.Visibility = Visibility.Visible;
                windowStateChanged = true;
            }
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                windowStateChanged = true;
            }
            // 窗口阴影厚度（左、右、下，值是实际像素数，与系统缩放率无关）
            int actualThickness = CommonUtils.GetWndShadowlessRect(
                this._interopHelper.Handle, out RECT rectangle);
            if (actualThickness == -1)
            {
                return windowStateChanged;
            }
            double scalingFactor = CommonUtils.GetScreenScalingFactor();
            double left = rectangle.left / scalingFactor;
            double top = rectangle.top / scalingFactor;
            double right = rectangle.right / scalingFactor;
            double bottom = rectangle.bottom / scalingFactor;
            // 因为 WPF 窗口的位置属性与缩放率相关，所以要计算与缩放率相关的阴影厚度
            double thickness = actualThickness / scalingFactor;
            if (left < 0.0)
            {
                this.Left = -thickness;
                windowStateChanged = true;
            }
            else if (right > SystemParameters.WorkArea.Width)
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width + thickness;
                windowStateChanged = true;
            }
            if (top < 0.0)
            {
                this.Top = 0.0;
                windowStateChanged = true;
            }
            else if (bottom > SystemParameters.WorkArea.Height)
            {
                this.Top = SystemParameters.WorkArea.Height - this.Height + thickness;
                windowStateChanged = true;
            }
            return windowStateChanged;
        }

        private void CommandPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }

        private void FiltersItemPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is AbsHashViewFilter filter)
            {
                this.model.SelectedFilter = filter;
            }
        }
    }
}
