using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HashCalculator
{
    public static class ExecTargetAttach
    {
        public static readonly DependencyProperty MonitoringProperty =
            DependencyProperty.RegisterAttached(
                "Monitoring",
                typeof(bool),
                typeof(ExecTargetAttach),
                new PropertyMetadata(false, MonitoringCallBack)
            );

        public static bool GetMonitoring(DependencyObject obj)
        {
            return (bool)obj.GetValue(MonitoringProperty);
        }

        public static void SetMonitoring(DependencyObject obj, bool value)
        {
            obj.SetValue(MonitoringProperty, value);
        }

        private static void MonitoringCallBack(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is MultiSelector multiSelector)
            {
                if (!(bool)e.OldValue && (bool)e.NewValue)
                {
                    // 同步在未订阅 SelectionChanged 时发生的选择变动
                    // 同步的意思是：刷新 ItemsSource 子项的 IsExecutionTarget 的值
                    // 使该值在不显示【操作目标】列时与主窗口所选择的行相一一对应
                    MultiSelectorSelectionChanged(obj, null);
                    multiSelector.SelectionChanged += MultiSelectorSelectionChanged;
                }
                else if ((bool)e.OldValue && !(bool)e.NewValue)
                {
                    multiSelector.SelectionChanged -= MultiSelectorSelectionChanged;
                    // 理论上在取消订阅 SelectionChanged 后也应该同步，但没必要
                    // 主窗口 State == Started 导致的取消会在 State != Started 时进入上面分支同步
                    // 而使用筛选与操作面板的选择按钮导致取消时也会由按钮动作进行同步
                }
            }
        }

        private static void MultiSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is MultiSelector multiSelector &&
                multiSelector.ItemsSource is ICollectionView collection &&
                multiSelector.SelectedItems is IList selectedItems)
            {
                foreach (HashViewModel model in collection)
                {
                    model.IsExecutionTarget = selectedItems.Contains(model);
                }
            }
        }
    }
}
