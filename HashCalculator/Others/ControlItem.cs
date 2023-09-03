namespace HashCalculator
{
    public class ControlItem
    {
        public ControlItem(string name, object value)
        {
            this.Display = name;
            this.ItemValue = value;
        }

        public string Display { get; }

        public object ItemValue { get; }

        public bool Selected { get; set; }
    }
}
