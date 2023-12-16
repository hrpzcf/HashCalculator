using System.Windows;

namespace HashCalculator
{
    public partial class ShellSubmenuModifier : Window
    {
        public static ShellSubmenuModifier This { get; private set; }

        public ShellSubmenuModifier(HcCtxMenuModel hcCtxMenuModel)
        {
            This = this;
            this.DataContext = hcCtxMenuModel;
            this.InitializeComponent();
        }
    }
}
