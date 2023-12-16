using System.Windows;

namespace HashCalculator
{
    public partial class ShellMenuModifier : Window
    {
        public static ShellMenuModifier This { get; private set; }

        public ShellMenuModifier()
        {
            This = this;
            this.DataContext = new ShellMenuModifierModel(this);
            this.InitializeComponent();
        }
    }
}
