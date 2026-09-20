using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HashCalculator.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace HashCalculator.Views.Pages;

public partial class AlgosPanelPage : Page, INavigableView<AlgorithmsModel>
{
    private const string AlgoDragDataFormat = "HashCalculator.AlgoInOutModel";

    private Point dragStartPoint;
    private AlgoInOutModel dragPendingAlgo;

    public AlgorithmsModel ViewModel { get; }

    public AlgosPanelPage(AlgorithmsModel model)
    {
        this.ViewModel = model;
        this.DataContext = this.ViewModel;
        this.InitializeComponent();
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
        DragDrop.DoDragDrop(grid, dragData, DragDropEffects.Move);
    }

    private void AlgoItemDragLeave(object sender, DragEventArgs e)
    {
        if (sender is not Grid grid ||
            FindInsertionLine(grid) is not Rectangle leavingLine)
        {
            return;
        }
        // 卡片内部的子元素（CardControl、Header、ToggleSwitch）之间切换也会触发 DragLeave，
        // 只有鼠标真的移出卡片边界（落到卡片间的间隙）时才隐藏插入线，避免插入线闪烁。
        Point position = e.GetPosition(grid);
        if (position.X < 0 || position.Y < 0 ||
            position.X > grid.ActualWidth || position.Y > grid.ActualHeight)
        {
            leavingLine.Visibility = Visibility.Collapsed;
        }
    }

    private void AlgoItemDrop(object sender, DragEventArgs e)
    {
        if (sender is not Grid grid)
        {
            return;
        }
        if (FindInsertionLine(grid) is Rectangle droppedLine)
        {
            droppedLine.Visibility = Visibility.Collapsed;
        }
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
        if (sender is not Grid grid || !e.Data.GetDataPresent(AlgoDragDataFormat))
        {
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        ShowInsertionLine(grid, IsDropAfterMiddle(e, grid));
    }

    private static void ShowInsertionLine(Grid grid, bool dropAfterMiddle)
    {
        if (FindInsertionLine(grid) is not Rectangle insertionLine)
        {
            return;
        }
        insertionLine.VerticalAlignment = dropAfterMiddle ?
            VerticalAlignment.Bottom : VerticalAlignment.Top;
        insertionLine.Visibility = Visibility.Visible;
    }

    private static Rectangle FindInsertionLine(Grid grid)
    {
        foreach (UIElement child in grid.Children)
        {
            if (child is Rectangle insertionLine)
            {
                return insertionLine;
            }
        }
        return default(Rectangle);
    }

    private static bool IsDropAfterMiddle(DragEventArgs e, FrameworkElement element)
    {
        return e.GetPosition(element).Y > element.ActualHeight / 2;
    }
}
