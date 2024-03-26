using System;
using System.Windows;
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
            this.model.ClearFiltersAndRefresh();
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
            if (!CommonUtils.GetWindowRectWithoutShadedArea(
                this._interopHelper.Handle, out RECT rectangle))
            {
                return windowStateChanged;
            }
            // 窗口左、右、下阴影厚度（上无阴影厚度）
            double thickness = rectangle.left - this.Left - 1;
            if (rectangle.left < 0)
            {
                this.Left = -thickness;
                windowStateChanged = true;
            }
            else if (rectangle.right > SystemParameters.WorkArea.Width)
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width + thickness;
                windowStateChanged = true;
            }
            if (rectangle.top < 0)
            {
                this.Top = 0.0;
                windowStateChanged = true;
            }
            else if (rectangle.bottom > SystemParameters.WorkArea.Height)
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
    }
}
