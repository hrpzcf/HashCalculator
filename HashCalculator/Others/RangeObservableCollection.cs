using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace HashCalculator;

/// <summary>
/// 支持 O(n) 批量增删且只触发一次 Reset 通知的 ObservableCollection 派生类。
/// 适用于大数据量场景（如主表格数千行）的批量增删，避免逐项通知导致的性能问题。
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
    /// 批量删除符合条件的项，只触发一次 Reset 通知。
    /// 内部优先使用 List&lt;T&gt;.RemoveAll 实现 O(n) 一次扫描；
    /// 若底层 Items 不是 List&lt;T&gt;，则退回 Clear + Add 的 O(n) 重建方式。
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        HashSet<T> itemSet = new HashSet<T>(items);
        if (itemSet.Count == 0)
        {
            return;
        }
        if (this.Items is List<T> list)
        {
            int removedCount = list.RemoveAll(i => itemSet.Contains(i));
            if (removedCount == 0)
            {
                return;
            }
        }
        else
        {
            // 兜底：底层不是 List<T> 时用 Clear + Add 重建
            List<T> kept = this.Items.Where(i => !itemSet.Contains(i)).ToList();
            if (kept.Count == this.Items.Count)
            {
                return;
            }
            this.Items.Clear();
            foreach (T item in kept)
            {
                this.Items.Add(item);
            }
        }
        this.NotifyRangeObservableCollectionReset();
    }

    /// <summary>
    /// 批量添加项，只触发一次 Reset 通知。
    /// 直接操作底层 Items 绕过逐项通知。
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        bool added = false;
        foreach (T item in items)
        {
            this.Items.Add(item);
            added = true;
        }
        if (!added)
        {
            return;
        }
        this.NotifyRangeObservableCollectionReset();
    }

    /// <summary>
    /// 批量替换集合内容（清空后批量添加），只触发一次 Reset 通知。
    /// </summary>
    public void ReplaceRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.Items.Clear();
        foreach (T item in items)
        {
            this.Items.Add(item);
        }
        this.NotifyRangeObservableCollectionReset();
    }

    /// <summary>
    /// 触发一次 Reset 通知并刷新 Count 和 Item[] 属性，
    /// 供批量操作完成后统一通知订阅者使用。
    /// </summary>
    private void NotifyRangeObservableCollectionReset()
    {
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Count)));
        this.OnPropertyChanged(new PropertyChangedEventArgs(IndexerPropertyName));
    }
}
