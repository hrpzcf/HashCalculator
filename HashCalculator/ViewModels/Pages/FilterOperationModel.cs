using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HashCalculator.Others;
using HashCalculator.Views.Windows;

namespace HashCalculator.ViewModels.Pages;

public class FilterOperationModel : BaseViewModel
{
    private AbsHashesCmder selectedCmder;
    private AbsHashViewFilter selectedFilter;
    private RelayCommand refreshFiltersCmd;
    private RelayCommand moveFilterUpCmd;
    private RelayCommand moveFilterDownCmd;
    private RelayCommand clearFilterSelectionCmd;
    private bool _isFiltersApplied = false;

    private ICollectionView BoundDataGridView { get; }

    public bool IsInformationBarOpened { get; set; } = true;

    public AbsHashesCmder SelectedCmder
    {
        get => this.selectedCmder;
        set
        {
            this.SetPropNotify(ref this.selectedCmder, value);
            this.NotifyPropertyChanged(nameof(this.IsInformationBarOpened));
        }
    }

    public AbsHashViewFilter SelectedFilter
    {
        get => this.selectedFilter;
        set
        {
            this.SetPropNotify(ref this.selectedFilter, value);
            this.NotifyPropertyChanged(nameof(this.IsInformationBarOpened));
        }
    }

    public bool IsFiltersApplied
    {
        get => this._isFiltersApplied;
        set => this.SetPropNotify(ref this._isFiltersApplied, value);
    }

    public AbsHashesCmder SelectTableLinesCmder { get; }

    public ObservableCollection<AbsHashesCmder> HashModelCmders { get; }

    public ObservableCollection<AbsHashViewFilter> HashModelFilters { get; }

    public FilterOperationModel(ICollectionView view)
    {
        this.BoundDataGridView = view;
        this.SelectTableLinesCmder = new SelectTargetsCmder(this);
        this.HashModelCmders = new ObservableCollection<AbsHashesCmder>()
        {
            new DeleteFileCmder(),
            new RenameFileCmder(),
            new MarkFilesCmder(),
            new RestoreFilesCmder(),
        };
        this.HashModelFilters = new ObservableCollection<AbsHashViewFilter>()
        {
            new FileIndexFilter(),
            new HashAlgoFilter(),
            new CmpResultFilter(),
            new HashingTaskResultFilter(),
            new SerialNumberFilter(),
            new FileSizeFilter(),
            new HashStringFilter(),
            new FileNameFilter(),
            // 这两个筛选器因其特殊性需最后应用，否则可能得不到预期结果
            new SameDirFilesFilter(),
            new EqualHashByteFilter(),
        };
    }

    public FilterOperationModel() : this(HashModelStore.HashViewModelsView)
    {
    }

    public void ResetFiltersAndRefresh()
    {
        foreach (AbsHashViewFilter filter in this.HashModelFilters)
        {
            filter.Reset();
        }
        // 传入 false 表示不筛选
        this.RefreshFiltersAction(false);
    }

    private void MoveFilterUpAction(object param)
    {
        int index;
        if ((index = this.HashModelFilters.IndexOf(this.SelectedFilter)) != -1 && index > 0)
        {
            int previous = index - 1;
            AbsHashViewFilter selected = this.SelectedFilter;
            this.HashModelFilters[index] = this.HashModelFilters[previous];
            this.HashModelFilters[previous] = selected;
            this.SelectedFilter = selected;
        }
    }

    public ICommand MoveFilterUpCmd
    {
        get
        {
            this.moveFilterUpCmd ??= new RelayCommand(this.MoveFilterUpAction);
            return this.moveFilterUpCmd;
        }
    }

    private void MoveFilterDownAction(object param)
    {
        int index;
        if ((index = this.HashModelFilters.IndexOf(this.SelectedFilter)) != -1 && index < this.HashModelFilters.Count - 1)
        {
            int nextOne = index + 1;
            AbsHashViewFilter selected = this.SelectedFilter;
            this.HashModelFilters[index] = this.HashModelFilters[nextOne];
            this.HashModelFilters[nextOne] = selected;
            this.SelectedFilter = selected;
        }
    }

    public ICommand MoveFilterDownCmd
    {
        get
        {
            this.moveFilterDownCmd ??= new RelayCommand(this.MoveFilterDownAction);
            return this.moveFilterDownCmd;
        }
    }

    private void ClearFilterSelectionAction(object param)
    {
        foreach (AbsHashViewFilter filter in this.HashModelFilters)
        {
            filter.Selected = false;
        }
    }

    public ICommand ClearFilterSelectionCmd
    {
        get
        {
            this.clearFilterSelectionCmd ??= new RelayCommand(this.ClearFilterSelectionAction);
            return this.clearFilterSelectionCmd;
        }
    }

    private async void RefreshFiltersAction(object param)
    {
        if (!Settings.Current.IsFiltersAndCmdersIdle)
        {
            return;
        }
        Settings.Current.IsFiltersAndCmdersIdle = false;
        this.SelectTableLinesCmder.Reset();
        foreach (AbsHashesCmder cmder in this.HashModelCmders)
        {
            cmder.Reset();
        }
        int appliedFiltersCount = 0;
        bool filteringShouldBeApplied = (param is not bool instruction) || instruction;
        await Task.Run(() =>
        {
            foreach (HashViewModel model in HashModelStore.HashViewModels)
            {
                model.Matched = true;
                model.FileIndex = null;
                model.HashGroupID = null;
                model.EmbeddedHashGroupID = null;
                model.FolderGroupID = null;
            }
            if (filteringShouldBeApplied)
            {
                foreach (AbsHashViewFilter filter in this.HashModelFilters)
                {
                    if (filter.Selected)
                    {
                        try
                        {
                            filter.FilterObjects(HashModelStore.HashViewModels);
                            appliedFiltersCount++;
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                NotificationSender.ShowMessageBox(MainWindow.Current,
                                    "筛选出错", $"筛选器名：{filter.Display}\n错误详情：{ex.Message}");
                            });
                        }
                    }
                }
            }
        });
        using (this.BoundDataGridView.DeferRefresh())
        {
            this.BoundDataGridView.SortDescriptions.Clear();
            this.BoundDataGridView.GroupDescriptions.Clear();
            this.BoundDataGridView.Filter = null;
            if (filteringShouldBeApplied)
            {
                bool anyFilterSelected = false;
                foreach (AbsHashViewFilter filter in this.HashModelFilters)
                {
                    if (filter.Selected)
                    {
                        anyFilterSelected = true;
                        this.BoundDataGridView.SortDescriptions.Extend(filter.SortDescriptions);
                        this.BoundDataGridView.GroupDescriptions.Extend(filter.GroupDescriptions);
                    }
                }
                if (anyFilterSelected)
                {
                    this.BoundDataGridView.Filter = filterObject => filterObject is HashViewModel model && model.Matched;
                }
            }
        }
        this.IsFiltersApplied = filteringShouldBeApplied;
        string promptMessage = filteringShouldBeApplied ?
            $"已应用 {appliedFiltersCount} 个筛选器并刷新筛选视图。" : $"已取消筛选并刷新视图。";
        NotificationSender.SnackbarSecondary(promptMessage);
        Settings.Current.IsFiltersAndCmdersIdle = true;
    }

    public ICommand RefreshFiltersCmd
    {
        get
        {
            this.refreshFiltersCmd ??= new RelayCommand(this.RefreshFiltersAction);
            return this.refreshFiltersCmd;
        }
    }
}
