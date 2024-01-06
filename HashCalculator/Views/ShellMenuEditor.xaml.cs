using System.Windows;

namespace HashCalculator
{
    public partial class ShellMenuEditor : Window
    {
        public ShellMenuEditor()
        {
            this.DataContext = new ShellMenuEditorModel(this);
            this.InitializeComponent();
        }
    }
}
