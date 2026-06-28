using System.Windows;
using System.Windows.Controls;

namespace HashCalculator.Controls;

public class CardItemsControl : ItemsControl
{
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return false;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ContentPresenter();
    }
}
