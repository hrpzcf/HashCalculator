using System;
using System.Windows;
using System.Windows.Resources;

namespace HashCalculator
{
    public partial class UsingHelpWindow : Window
    {
        public UsingHelpWindow()
        {
            this.InitializeComponent();
            StreamResourceInfo stream = Application.GetResourceStream(
                new Uri("WebPages/Help.html", UriKind.Relative));
            this.uiWebBrowser_ShowUsingHelp.NavigateToStream(stream.Stream);
        }
    }
}
