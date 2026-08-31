using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace HashCalculator;

/// <summary>
/// 支持批量增删的 ObservableCollection 派生类。
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// WPF 绑定系统约定的索引器属性名，用于通知集合内容变更。
    /// </summary>
    private const string IndexerPropertyName = "Item[]";

    public RangeObservableCollection() : base() { }

    public RangeObservableCollection(IEnumerable<T> collection) : base(collection) { }

    public RangeObservableCollection(List<T> list) : base(list) { }

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
