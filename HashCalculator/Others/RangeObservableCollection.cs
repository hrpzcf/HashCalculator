using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace HashCalculator;

/// <summary>
/// 支持批量增删的 ObservableCollection 派生类。
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    private bool lazyCountDirty;

    /// <summary>
    /// 节流的集合元素数量，供界面绑定等只要求及时而非实时的场景使用。<br/>
    /// 无论元素被逐个添加/移除还是批量增删，本属性都按 100 毫秒的间隔更新一次，
    /// 避免大量逐个添加时频繁通知界面造成的开销。<br/>
    /// 与基类的 Count 不同：Count 每次变化都立即通知，用于需要实时性的绑定。
    /// </summary>
    public int LazyCount { get; private set; }
    private DispatcherTimer lazyCountReportTimer;

    /// <summary>
    /// WPF 绑定系统约定的索引器属性名，用于通知集合内容变更。
    /// </summary>
    private const string IndexerPropertyName = "Item[]";

    public RangeObservableCollection() : base()
    {
        this.InitializeLazyCount();
    }

    public RangeObservableCollection(IEnumerable<T> collection) : base(collection)
    {
        this.InitializeLazyCount();
    }

    public RangeObservableCollection(List<T> list) : base(list)
    {
        this.InitializeLazyCount();
    }

    /// <summary>
    /// 初始化 LazyCount 的节流上报：订阅自身的属性变更，使 Count 无论因逐个增删、
    /// 批量增删还是清空而变化，都能被统一捕获并按固定间隔节流上报。
    /// </summary>
    private void InitializeLazyCount()
    {
        this.PropertyChanged += this.OnCountChanged;
        this.lazyCountReportTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        this.lazyCountReportTimer.Tick += this.LazyCountReportTimerTick;
    }

    private void OnCountChanged(object sender, PropertyChangedEventArgs e)
    {
        // 只关注 Count：以"结果"为触发源，不必逐一覆盖增删改方法，覆盖更全面
        // 值实际未变化（例如增删数量相同）时无需上报
        if (e.PropertyName == nameof(this.Count) && this.LazyCount != this.Count)
        {
            this.lazyCountDirty = true;
            if (!this.lazyCountReportTimer.IsEnabled)
            {
                this.lazyCountReportTimer.Start();
            }
        }
    }

    private void LazyCountReportTimerTick(object sender, EventArgs e)
    {
        if (this.lazyCountDirty)
        {
            this.lazyCountDirty = false;
            this.LazyCount = this.Count;
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.LazyCount)));
        }
        this.lazyCountReportTimer.Stop();
    }

    /// <summary>
    /// 批量添加项。
    /// </summary>
    public void AddItems(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        int addedItemCount = 0;
        int newItemsBaseIndex = this.Items.Count;
        if (this.Items is List<T> values)
        {
            values.AddRange(items);
            addedItemCount = values.Count - newItemsBaseIndex;
        }
        else
        {
            foreach (T item in items)
            {
                this.Items.Add(item);
                addedItemCount++;
            }
        }
        if (addedItemCount > 0)
        {
            for (int i = 0; i < addedItemCount; i++)
            {
                int index = newItemsBaseIndex + i;
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, this.Items[index], index));
            }
            this.NotifyRangeObservableCollectionPropertyChanged();
        }
    }

    /// <summary>
    /// 批量删除项，根据删除比例自动选择 Reset 或逐项 Remove 通知。
    /// </summary>
    public void RemoveItems(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        HashSet<T> itemSet = new HashSet<T>(items);
        if (itemSet.Count == 0)
        {
            return;
        }
        int originalCount = this.Items.Count;
        List<(int Index, T Item)> itemsToRemove = new();
        for (int i = 0; i < originalCount; i++)
        {
            T item = this.Items[i];
            if (itemSet.Contains(item))
            {
                itemsToRemove.Add((i, item));
            }
        }
        int removedCount = itemsToRemove.Count;
        if (removedCount > 0)
        {
            // 从高位向低位逐个移除，保证索引不偏移
            for (int i = itemsToRemove.Count - 1; i >= 0; i--)
            {
                (int itemIndex, T item) = itemsToRemove[i];
                this.Items.RemoveAt(itemIndex);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, item, itemIndex));
            }
            this.NotifyRangeObservableCollectionPropertyChanged();
        }
    }

    /// <summary>
    /// 批量替换集合内容（清空后批量添加），始终触发 Reset 通知。
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.Items.Clear();
        if (items is List<T> values)
        {
            values.AddRange(items);
        }
        else
        {
            foreach (T item in items)
            {
                this.Items.Add(item);
            }
        }
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset));
        this.NotifyRangeObservableCollectionPropertyChanged();
    }

    /// <summary>
    /// 触发一次 Reset 通知并刷新 Count 和 Item[] 属性，
    /// 供批量操作完成后统一通知订阅者使用。
    /// </summary>
    private void NotifyRangeObservableCollectionPropertyChanged()
    {
        this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Count)));
        this.OnPropertyChanged(new PropertyChangedEventArgs(IndexerPropertyName));
    }
}
