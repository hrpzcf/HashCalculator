using System.Windows;

namespace HashCalculator
{
    public partial class ShellSubmenuEditor : Window
    {
        public static ShellSubmenuEditor Current { get; private set; }

        public ShellSubmenuEditor(HcCtxMenuModel hcCtxMenuModel)
        {
            Current = this;
            this.DataContext = hcCtxMenuModel;
            this.InitializeComponent();
        }
    }
}
