namespace HashCalculator
{
    public class GenericItemModel
    {
        public GenericItemModel(string name, object value)
        {
            this.Display = name;
            this.ItemValue = value;
        }

        public GenericItemModel(string name, object param, object value)
        {
            this.Display = name;
            this.Param = param;
            this.ItemValue = value;
        }

        public string Display { get; }

        public object ItemValue { get; }

        public object Param { get; }

        public bool Selected { get; set; }
    }
}
