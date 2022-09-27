using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace HashCalculator
{
    /// <summary>
    /// UsingHelp.xaml 的交互逻辑
    /// </summary>
    public partial class UsingHelp : Window
    {
        public UsingHelp()
        {
            InitializeComponent();
            StreamResourceInfo stream = 
                Application.GetResourceStream(new Uri("/HtmlFiles/UsingHelp.html", UriKind.Relative));
            this.uiWebBrowser_ShowUsingHelp.NavigateToStream(stream.Stream);
        }
    }
}
