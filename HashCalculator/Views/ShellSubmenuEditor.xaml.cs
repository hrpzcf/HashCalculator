using System.Windows;

namespace HashCalculator
{
    public partial class ShellSubmenuEditor : Window
    {
        public static ShellSubmenuEditor This { get; private set; }

        public ShellSubmenuEditor(HcCtxMenuModel hcCtxMenuModel)
        {
            This = this;
            this.DataContext = hcCtxMenuModel;
            this.InitializeComponent();
        }
    }
}
