namespace HashCalculator
{
    public class ComboItem
    {
        public ComboItem(string name, object value)
        {
            this.Display = name;
            this.ItemValue = value;
        }

        public string Display { get; }

        public object ItemValue { get; }
    }
}
