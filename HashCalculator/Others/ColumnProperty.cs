using System.Windows.Controls;

namespace HashCalculator
{
    public class ColumnProperty
    {
        public ColumnProperty(int index, DataGridLength width)
        {
            this.Index = index;
            this.Width = width;
        }

        public int Index { get; set; }

        public DataGridLength Width { get; set; }
    }
}
