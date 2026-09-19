namespace HashCalculator;

public class GroupedItemModel
{
    public GroupedItemModel(string name, GenericItemModel[] items)
    {
        this.Display = name;
        this.Items = items;
    }

    public string Display { get; }

    public GenericItemModel[] Items { get; }
}
