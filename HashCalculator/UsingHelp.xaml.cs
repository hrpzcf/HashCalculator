using System;
using System.Windows;
using System.Windows.Resources;

namespace HashCalculator
{
    public partial class UsingHelpWindow : Window
    {
        public UsingHelpWindow()
        {
            InitializeComponent();
            StreamResourceInfo stream = Application.GetResourceStream(
                new Uri("Others/UsingHelp.html", UriKind.Relative));
            this.uiWebBrowser_ShowUsingHelp.NavigateToStream(stream.Stream);
        }
    }
}
