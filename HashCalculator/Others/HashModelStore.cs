using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace HashCalculator.Others;

public static class HashModelStore
{
    static HashModelStore()
    {
        HashViewModels = new RangeObservableCollection<HashViewModel>();
        HashViewModelsSource = new CollectionViewSource();
        HashViewModelsSource.Source = HashViewModels;
        HashViewModelsView = HashViewModelsSource.View;
    }

    /// <summary>
    /// 用于在 xaml 文件内给 DataGrid 的 ItemsSource 属性绑定。
    /// 因为 ItemsSource 直接绑定到 HashViewModels，对视图的分组等操作不会生效。
    /// </summary>
    public static CollectionViewSource HashViewModelsSource { get; }

    /// <summary>
    /// 此属性相当于 HashViewModelsSource.View 的简写。
    /// </summary>
    public static ICollectionView HashViewModelsView { get; }

    public static RangeObservableCollection<HashViewModel> HashViewModels { get; }
}
