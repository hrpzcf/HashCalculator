namespace HashCalculator
{
    public class ComboBoxItem
    {
        public ComboBoxItem(string name, object value)
        {
            this.Display = name;
            this.ItemValue = value;
        }

        public string Display { get; }

        public object ItemValue { get; }
    }
}
