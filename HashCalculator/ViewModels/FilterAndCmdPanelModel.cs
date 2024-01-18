using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class FilterAndCmdPanelModel : NotifiableModel
    {
        private AbsHashesCmder selectedCmder;
        private AbsHashViewFilter selectedFilter;
        private RelayCommand refreshFiltersCmd;
        private RelayCommand filterChangedCmd;
        private RelayCommand moveFilterUpCmd;
        private RelayCommand moveFilterDownCmd;
        private RelayCommand clearFilterSelectionCmd;

        private ICollectionView BoundDataGridView { get; }

        public AbsHashesCmder SelectedCmder
        {
            get
            {
                return this.selectedCmder;
            }
            set
            {
                this.SetPropNotify(ref this.selectedCmder, value);
            }
        }

        public AbsHashViewFilter SelectedFilter
        {
            get
            {
                return this.selectedFilter;
            }
            set
            {
                this.SetPropNotify(ref this.selectedFilter, value);
            }
        }

        public AbsHashesCmder SelectTableLinesCmder { get; }

        public ObservableCollection<AbsHashesCmder> HashModelCmders { get; }

        public ObservableCollection<AbsHashViewFilter> HashModelFilters { get; }

        public FilterAndCmdPanelModel(ICollectionView view)
        {
            this.BoundDataGridView = view;
            this.SelectTableLinesCmder = new SelectTargetsCmder();
            this.HashModelCmders = new ObservableCollection<AbsHashesCmder>()
            {
                new DeleteFileCmder(),
                new RenameFileCmder(),
                new MarkFilesCmder(),
                new RestoreFilesCmder(),
            };
            this.HashModelFilters = new ObservableCollection<AbsHashViewFilter>()
            {
                new HashAlgoFilter(),
                new CmpResultFilter(),
                new HashingTaskResultFilter(),
                new SerialNumberFilter(),
                new FileIndexFilter(),
                new FileSizeFilter(),
                new HashStringFilter(),
                new FileNameFilter(),           
                // 这两个筛选器因其特殊性需最后应用，否则可能得不到预期结果
                new SameDirFilesFilter(),
                new EqualHashByteFilter(),
            };
        }

        public FilterAndCmdPanelModel() : this(MainWndViewModel.HashViewModelsView)
        {
        }

        public void ClearFiltersAndRefresh()
        {
            foreach (AbsHashViewFilter filter in this.HashModelFilters)
            {
                filter.Reset();
            }
            this.RefreshFiltersAction(false);  // 传入 false 表示不筛选
        }

        private void FilterChangedAction(object param)
        {
            if (param is AbsHashViewFilter filter)
            {
                if (!filter.Selected)
                {
                    filter.Reset();
                }
                this.SelectedFilter = filter;
            }
        }

        public ICommand FilterChangedCmd
        {
            get
            {
                if (this.filterChangedCmd == null)
                {
                    this.filterChangedCmd = new RelayCommand(this.FilterChangedAction);
                }
                return this.filterChangedCmd;
            }
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
                if (this.moveFilterUpCmd == null)
                {
                    this.moveFilterUpCmd = new RelayCommand(this.MoveFilterUpAction);
                }
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
                if (this.moveFilterDownCmd == null)
                {
                    this.moveFilterDownCmd = new RelayCommand(this.MoveFilterDownAction);
                }
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
                if (this.clearFilterSelectionCmd == null)
                {
                    this.clearFilterSelectionCmd = new RelayCommand(this.ClearFilterSelectionAction);
                }
                return this.clearFilterSelectionCmd;
            }
        }

        private async void RefreshFiltersAction(object param)
        {
            if (!Settings.Current.FilterOrCmderEnabled)
            {
                return;
            }
            Settings.Current.FilterOrCmderEnabled = false;
            foreach (AbsHashesCmder cmder in this.HashModelCmders)
            {
                cmder.Reset();
            }
            bool filteringShouldBeApplied = !(param is bool instruction) || instruction;
            await Task.Run(() =>
            {
                foreach (HashViewModel model in MainWndViewModel.HashViewModels)
                {
                    model.Matched = true;
                    model.FileIndex = null;
                    model.GroupId = null;
                    model.FdGroupId = null;
                }
                if (filteringShouldBeApplied)
                {
                    foreach (AbsHashViewFilter filter in this.HashModelFilters)
                    {
                        if (filter.Selected)
                        {
                            try
                            {
                                filter.FilterObjects(MainWndViewModel.HashViewModels);
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(MainWindow.This,
                                        $"筛选器名：{filter.Display}\n错误详情：{ex.Message}", "筛选出错",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
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
            Settings.Current.FilterOrCmderEnabled = true;
        }

        public ICommand RefreshFiltersCmd
        {
            get
            {
                if (this.refreshFiltersCmd == null)
                {
                    this.refreshFiltersCmd = new RelayCommand(this.RefreshFiltersAction);
                }
                return this.refreshFiltersCmd;
            }
        }
    }
}
