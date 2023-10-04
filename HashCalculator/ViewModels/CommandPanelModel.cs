using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class CommandPanelModel : NotifiableModel
    {
        private readonly ICollectionView BoundDataGridView;
        private RelayCommand refreshViewCmd;
        private RelayCommand filterChangedCmd;

        public HashViewCmder[] HashModelCmders { get; } = new HashViewCmder[]
        {
            new SelectFileCmder(),          // 0
            new DeleteFileCmder(),          // 1
            new RenameFileCmder(),          // 2
        };

        public HashViewFilter[] HashModelFilters { get; } = new HashViewFilter[]
        {
            new HashAlgoFilter(),           // 0
            new CmpResultFilter(),          // 1
            new HashingTaskResultFilter(),  // 2
            new SerialNumberFilter(),       // 3
            new DistinctFilesFilter(),      // 4
            new FileSizeFilter(),           // 5
            new HashStringFilter(),         // 6
            new FileNameFilter(),           // 7
            // 这个筛选器因其特殊性需最后应用，否则结果不正确
            new SameDirFilesFilter(),       // 8
            new EqualHashByteFilter(),      // 9
        };

        public CommandPanelModel(ICollectionView view)
        {
            this.BoundDataGridView = view;
        }

        public CommandPanelModel() : this(MainWndViewModel.HashViewModelsView)
        {
        }

        public void ClearFiltersAndRefresh()
        {
            foreach (HashViewFilter filter in this.HashModelFilters)
            {
                filter.Reset();
            }
            this.RefreshViewAction(false);  // 传入 bool 类型(false)表示不筛选
        }

        private void FilterChangedAction(object param)
        {
            if (param is HashViewFilter filter && !filter.Selected)
            {
                filter.Reset();
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

        private async void RefreshViewAction(object param)
        {
            if (!Settings.Current.FilterOrCmderEnabled)
            {
                return;
            }
            Settings.Current.FilterOrCmderEnabled = false;
            foreach (HashViewCmder cmder in this.HashModelCmders)
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
                    foreach (HashViewFilter filter in this.HashModelFilters)
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
                    foreach (HashViewFilter filter in this.HashModelFilters)
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

        public ICommand RefreshViewCmd
        {
            get
            {
                if (this.refreshViewCmd == null)
                {
                    this.refreshViewCmd = new RelayCommand(this.RefreshViewAction);
                }
                return this.refreshViewCmd;
            }
        }
    }
}
