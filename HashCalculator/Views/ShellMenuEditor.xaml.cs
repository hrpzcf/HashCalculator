using System.Windows;

namespace HashCalculator
{
    public partial class ShellMenuEditor : Window
    {
        public static ShellMenuEditor This { get; private set; }

        public ShellMenuEditor()
        {
            This = this;
            this.DataContext = new ShellMenuEditorModel(this);
            this.InitializeComponent();
        }
    }
}
