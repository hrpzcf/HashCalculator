using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace HashCalculator.Views.Pages;

public partial class AlgosPanelPage : Page, INavigableView<AlgorithmsModel>
{
    // 插入线两端各留出的距离，避开卡片 4px 的圆角
    private const double InsertionLineInset = 4;
    private const string AlgoDragDataFormat = "HashCalculator.AlgoInOutModel";
    // 自动滚动：鼠标进入边缘多少像素内触发、每次滚动像素数的下限与上限、定时器间隔
    private const double AutoScrollHotZone = 40;
    private const double AutoScrollMaxStep = 30;
    private const double AutoScrollMinStep = 5;
    private const int AutoScrollIntervalMs = 30;

    private bool blankDropAfterTarget;
    private Point dragStartPoint;
    private AlgoInOutModel dragPendingAlgo;
    // 鼠标落在卡片间隙或空白处时的插入目标
    private AlgoInOutModel blankDropTarget;
    // 被拖动的卡片，仅用于卡片因滚动而平移时刷新虚线框的位置
    private Grid draggedItem;
    // 自动滚动：定时器、最近一次鼠标位置（相对插入线所在画布）、每次滚动的像素数（带方向）
    private readonly DispatcherTimer autoScrollTimer = new DispatcherTimer();
    private double autoScrollStep;
    private Point lastMousePosition;

    public AlgorithmsModel ViewModel { get; }

    public AlgosPanelPage(AlgorithmsModel model)
    {
        this.ViewModel = model;
        this.DataContext = this.ViewModel;
        this.InitializeComponent();
        this.autoScrollTimer.Interval = TimeSpan.FromMilliseconds(AutoScrollIntervalMs);
        this.autoScrollTimer.Tick += this.AutoScrollTimerTick;
    }

    private void AlgoItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.dragPendingAlgo = null;
        if (sender is not Grid grid || grid.DataContext is not AlgoInOutModel algo ||
            IsUnderToggleSwitch(e.OriginalSource as DependencyObject))
        {
            return;
        }
        this.dragPendingAlgo = algo;
        this.dragStartPoint = e.GetPosition(null);
    }

    private static bool IsUnderToggleSwitch(DependencyObject element)
    {
        for (DependencyObject current = element; current != null;
            current = VisualTreeHelper.GetParent(current))
        {
            if (current is ToggleSwitch)
            {
                return true;
            }
        }
        return false;
    }

    private void AlgoItemPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (this.dragPendingAlgo == null ||
            e.LeftButton != MouseButtonState.Pressed ||
            sender is not Grid grid)
        {
            return;
        }
        Point currentPoint = e.GetPosition(null);
        // 位移没有达到系统要求的拖拽距离时不启动拖拽，以免影响开关的点击
        if (Math.Abs(currentPoint.Y - this.dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance &&
            Math.Abs(currentPoint.X - this.dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance)
        {
            return;
        }
        AlgoInOutModel draggedAlgo = this.dragPendingAlgo;
        this.dragPendingAlgo = null;
        DataObject dragData = new DataObject(AlgoDragDataFormat, draggedAlgo);
        this.ShowDragOutline(grid);
        DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);
        // 拖拽结束（含按 Esc 取消）后收起虚线框和插入线
        this.EndDragVisual();
    }

    // 拖动时给被拖动的卡片画一个虚线框，标记它正在被拖动
    private void ShowDragOutline(Grid item)
    {
        this.draggedItem = item;
        this.DragOutline.Visibility = Visibility.Visible;
        this.UpdateDragOutline();
    }

    // 卡片会因为滚动而整体平移，虚线框的位置要跟着刷新
    private void UpdateDragOutline()
    {
        if (this.draggedItem == null)
        {
            return;
        }
        Point itemOrigin = this.draggedItem.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
        this.DragOutline.Width = this.draggedItem.ActualWidth;
        this.DragOutline.Height = this.draggedItem.ActualHeight;
        Canvas.SetLeft(this.DragOutline, itemOrigin.X);
        Canvas.SetTop(this.DragOutline, itemOrigin.Y);
    }

    private void EndDragVisual()
    {
        this.draggedItem = null;
        this.StopAutoScroll();
        this.DragOutline.Visibility = Visibility.Collapsed;
        this.InsertionLine.Visibility = Visibility.Collapsed;
    }

    private void AlgoItemDrop(object sender, DragEventArgs e)
    {
        if (sender is not Grid grid)
        {
            return;
        }
        this.InsertionLine.Visibility = Visibility.Collapsed;
        if (!e.Data.GetDataPresent(AlgoDragDataFormat) ||
            e.Data.GetData(AlgoDragDataFormat) is not AlgoInOutModel source ||
            grid.DataContext is not AlgoInOutModel target)
        {
            return;
        }
        AlgorithmsModel.MoveAlgo(source, target, IsDropAfterMiddle(e, grid));
        e.Handled = true;
    }

    private void AlgoItemDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(AlgoDragDataFormat))
        {
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        this.UpdateDragFeedback(e.GetPosition(this.InsertionLineLayer));
    }

    // 按鼠标位置刷新插入线，并在鼠标靠近左右边缘时启动自动滚动
    private bool UpdateDragFeedback(Point mouse)
    {
        this.lastMousePosition = mouse;
        this.UpdateDragOutline();
        bool hasDropTarget = this.UpdateInsertionVisual(mouse);
        this.UpdateAutoScroll(mouse);
        return hasDropTarget;
    }

    // 鼠标压在某张卡片上时按上下半区决定插入位置，否则按鼠标下方最近的卡片决定
    private bool UpdateInsertionVisual(Point mouse)
    {
        WrapPanel itemsPanel = FindVisualDescendant<WrapPanel>(this.AlgoItemsControl);
        if (itemsPanel == null)
        {
            return false;
        }
        this.blankDropTarget = null;
        Grid hoveredItem = this.FindItemAt(itemsPanel, mouse);
        if (hoveredItem != null)
        {
            Point itemOrigin = hoveredItem.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
            this.ShowInsertionLine(
                hoveredItem,
                mouse.Y > itemOrigin.Y + (hoveredItem.ActualHeight / 2));
            return true;
        }
        if (this.TryShowInsertionLineForBlankArea(itemsPanel, mouse))
        {
            return true;
        }
        this.InsertionLine.Visibility = Visibility.Collapsed;
        return false;
    }

    private Grid FindItemAt(WrapPanel itemsPanel, Point mouse)
    {
        foreach (UIElement child in itemsPanel.Children)
        {
            if (child is not ContentPresenter container || FindItemGrid(container) is not Grid item)
            {
                continue;
            }
            Point itemOrigin = item.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
            if (mouse.X >= itemOrigin.X &&
                mouse.X <= itemOrigin.X + item.ActualWidth &&
                mouse.Y >= itemOrigin.Y &&
                mouse.Y <= itemOrigin.Y + item.ActualHeight)
            {
                return item;
            }
        }
        return null;
    }

    // 鼠标进入算法区左右边缘的热区时开始滚动，越靠近边缘滚得越快
    private void UpdateAutoScroll(Point mouse)
    {
        double viewportWidth = this.InsertionLineLayer.ActualWidth;
        double edgeOffset;
        if (mouse.X < AutoScrollHotZone)
        {
            edgeOffset = mouse.X - AutoScrollHotZone;
        }
        else if (mouse.X > viewportWidth - AutoScrollHotZone)
        {
            edgeOffset = mouse.X - (viewportWidth - AutoScrollHotZone);
        }
        else
        {
            this.StopAutoScroll();
            return;
        }
        double ratio = Math.Min(1, Math.Abs(edgeOffset) / AutoScrollHotZone);
        this.autoScrollStep = Math.Sign(edgeOffset) *
            (AutoScrollMinStep + ((AutoScrollMaxStep - AutoScrollMinStep) * ratio));
        if (!this.autoScrollTimer.IsEnabled)
        {
            this.autoScrollTimer.Start();
        }
    }

    // 滚动会让所有卡片整体平移，因此每滚一步都要刷新虚线框，并按鼠标当前位置重算插入线
    private void AutoScrollTimerTick(object sender, EventArgs e)
    {
        double currentOffset = this.AlgoScrollViewer.HorizontalOffset;
        double nextOffset = Math.Clamp(
            currentOffset + this.autoScrollStep,
            0,
            this.AlgoScrollViewer.ScrollableWidth);
        if (nextOffset == currentOffset)
        {
            // 已经滚到尽头或者没有可滚动的内容，就没有必要继续
            this.StopAutoScroll();
            return;
        }
        this.AlgoScrollViewer.ScrollToHorizontalOffset(nextOffset);
        this.UpdateDragOutline();
        this.UpdateInsertionVisual(this.lastMousePosition);
    }

    private void StopAutoScroll()
    {
        this.autoScrollTimer.Stop();
        this.autoScrollStep = 0;
    }

    private void ShowInsertionLine(Grid hoveredItem, bool dropAfterMiddle)
    {
        Point itemOrigin = hoveredItem.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
        double lineTop = itemOrigin.Y;
        if (dropAfterMiddle)
        {
            // 插到该项之后：优先画在"同列下一项"的顶边，这样与命中下一项上半区时的显示位置完全一致；
            // 已经是列尾或末项时，画在本卡片底边内侧，表示插到末尾。
            Grid nextItem = FindFollowingItemInSameColumn(hoveredItem);
            lineTop = nextItem != null ?
                nextItem.TranslatePoint(new Point(0, 0), this.InsertionLineLayer).Y :
                itemOrigin.Y + hoveredItem.ActualHeight - this.InsertionLine.Height;
        }
        this.ShowInsertionLineAt(hoveredItem, lineTop);
    }

    private void ShowInsertionLineAt(Grid item, double lineTop)
    {
        Point itemOrigin = item.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
        this.InsertionLine.Width = Math.Max(0, item.ActualWidth - (InsertionLineInset * 2));
        Canvas.SetLeft(this.InsertionLine, itemOrigin.X + InsertionLineInset);
        Canvas.SetTop(this.InsertionLine, lineTop);
        this.InsertionLine.Visibility = Visibility.Visible;
    }

    private static Grid FindFollowingItemInSameColumn(Grid item)
    {
        if (VisualTreeHelper.GetParent(item) is not ContentPresenter container ||
            VisualTreeHelper.GetParent(container) is not WrapPanel panel)
        {
            return null;
        }
        int index = panel.Children.IndexOf(container);
        if (index < 0 ||
            index + 1 >= panel.Children.Count ||
            panel.Children[index + 1] is not ContentPresenter nextContainer)
        {
            return null;
        }
        // WrapPanel 按列填充，只有横向位置相同的下一项才在同一列的下一格
        // 这里必须都取 ContentPresenter 的原点：卡片 Grid 自身带有 16px 左边距，直接相减会恒不等于 0
        double horizontalOffset =
            nextContainer.TranslatePoint(new Point(0, 0), panel).X -
            container.TranslatePoint(new Point(0, 0), panel).X;
        return Math.Abs(horizontalOffset) > 0.5 ? null : FindItemGrid(nextContainer);
    }

    private static Grid FindItemGrid(ContentPresenter container)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(container); index++)
        {
            if (VisualTreeHelper.GetChild(container, index) is Grid grid)
            {
                return grid;
            }
        }
        return null;
    }

    private void PageDragOver(object sender, DragEventArgs e)
    {
        // 卡片的 DragOver 已经标记为已处理，能冒泡到这里说明鼠标落在卡片间隙或空白处
        if (!e.Data.GetDataPresent(AlgoDragDataFormat) ||
            !this.UpdateDragFeedback(e.GetPosition(this.InsertionLineLayer)))
        {
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    // 鼠标落在卡片间隙或空白处时，以同列中鼠标下方最近的一项为插入目标（插到它之前）；
    // 该列下方已经没有卡片时，以该列最后一项为插入目标（插到它之后）。
    private bool TryShowInsertionLineForBlankArea(WrapPanel itemsPanel, Point mouse)
    {
        Grid followingItem = null;
        double followingItemTop = double.MaxValue;
        Grid lastItemInColumn = null;
        double lastItemBottom = double.MinValue;
        foreach (UIElement child in itemsPanel.Children)
        {
            if (child is not ContentPresenter container || FindItemGrid(container) is not Grid item)
            {
                continue;
            }
            // 卡片自身带有 16px 的左边距，插入线还要在卡片两端内缩，这里把这段也算作所在列的范围
            Point itemOrigin = item.TranslatePoint(new Point(0, 0), this.InsertionLineLayer);
            if (mouse.X < itemOrigin.X - InsertionLineInset ||
                mouse.X > itemOrigin.X + item.ActualWidth)
            {
                continue;
            }
            double itemBottom = itemOrigin.Y + item.ActualHeight;
            if (mouse.Y >= itemOrigin.Y && mouse.Y <= itemBottom)
            {
                // 鼠标其实还在某一项的高度范围内（例如卡片左侧的留白），仍交给卡片的处理逻辑
                return false;
            }
            if (itemOrigin.Y > mouse.Y && itemOrigin.Y < followingItemTop)
            {
                followingItemTop = itemOrigin.Y;
                followingItem = item;
            }
            if (itemBottom > lastItemBottom)
            {
                lastItemBottom = itemBottom;
                lastItemInColumn = item;
            }
        }
        if (followingItem != null)
        {
            this.blankDropTarget = followingItem.DataContext as AlgoInOutModel;
            this.blankDropAfterTarget = false;
            this.ShowInsertionLineAt(followingItem, followingItemTop);
            return true;
        }
        if (lastItemInColumn != null)
        {
            this.blankDropTarget = lastItemInColumn.DataContext as AlgoInOutModel;
            this.blankDropAfterTarget = true;
            this.ShowInsertionLineAt(lastItemInColumn, lastItemBottom - this.InsertionLine.Height);
            return true;
        }
        return false;
    }

    private static T FindVisualDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T descendant)
            {
                return descendant;
            }
            if (FindVisualDescendant<T>(child) is T nestedDescendant)
            {
                return nestedDescendant;
            }
        }
        return null;
    }

    private void PageDrop(object sender, DragEventArgs e)
    {
        this.InsertionLine.Visibility = Visibility.Collapsed;
        AlgoInOutModel target = this.blankDropTarget;
        this.blankDropTarget = null;
        if (target == null ||
            !e.Data.GetDataPresent(AlgoDragDataFormat) ||
            e.Data.GetData(AlgoDragDataFormat) is not AlgoInOutModel source)
        {
            return;
        }
        AlgorithmsModel.MoveAlgo(source, target, this.blankDropAfterTarget);
        e.Handled = true;
    }

    private static bool IsDropAfterMiddle(DragEventArgs e, FrameworkElement element)
    {
        return e.GetPosition(element).Y > element.ActualHeight / 2;
    }
}
