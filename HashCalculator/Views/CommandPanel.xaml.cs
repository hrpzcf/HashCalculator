using System;
using System.Windows;

namespace HashCalculator
{
    public partial class CommandPanel : Window
    {
        public CommandPanel(EventHandler handler)
        {
            this.Closed += handler;
            this.Closed += this.PanelClosed;
            this.Loaded += (s, e) => { this.CheckPanelPosition(); };
            this.InitializeComponent();
        }

        private void PanelClosed(object sender, EventArgs e)
        {
            if (this.DataContext is CommandPanelModel model)
            {
                model.ClearSelectorsAndRefresh();
            }
        }

        public bool CheckPanelPosition()
        {
            if (this.Left < 0.0)
            {
                this.Left = 0.0;
                return true;
            }
            else if (this.Left + this.Width > SystemParameters.WorkArea.Width)
            {
                this.Left = SystemParameters.WorkArea.Width - this.Width;
                return true;
            }
            if (this.Top < 0.0)
            {
                this.Top = 0.0;
                return true;
            }
            else if (this.Top + this.Height > SystemParameters.WorkArea.Height)
            {
                this.Top = SystemParameters.WorkArea.Height - this.Height;
                return true;
            }
            return false;
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
