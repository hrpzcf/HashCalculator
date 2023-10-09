﻿using System;
using System.Windows;

namespace HashCalculator
{
    public partial class FilterAndCmdPanel : Window
    {
        private readonly FilterAndCmdPanelModel model;

        public static FilterAndCmdPanel This { get; private set; }

        public FilterAndCmdPanel(EventHandler handler)
        {
            this.model = new FilterAndCmdPanelModel();
            this.DataContext = this.model;
            This = this;
            this.Closed += handler;
            this.Closed += this.PanelClosed;
            this.Loaded += (s, e) => { this.CheckPanelPosition(); };
            this.InitializeComponent();
        }

        private void PanelClosed(object sender, EventArgs e)
        {
            this.model.ClearFiltersAndRefresh();
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
