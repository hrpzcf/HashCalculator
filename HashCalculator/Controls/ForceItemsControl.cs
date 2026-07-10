using System.Windows;
using System.Windows.Controls;

namespace HashCalculator.Controls;

public class ForceItemsControl : ItemsControl
{
    protected override bool IsItemItsOwnContainerOverride(object i)
    {
        return false;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ContentPresenter();
    }
}
