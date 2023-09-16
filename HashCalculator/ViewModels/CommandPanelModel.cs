using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HashCalculator
{
    internal class CommandPanelModel : NotifiableModel
    {
        private bool _refreshEnabled = true;
        private readonly ICollectionView BoundDataGridView;
        private RelayCommand refreshViewCmd;
        private RelayCommand filterChangedCmd;

        public HashViewCmder[] HashModelCmders { get; } = new HashViewCmder[]
        {
            new DelEqualHashFileCmder(),    // 0
        };

        public HashViewFilter[] HashModelFilters { get; } = new HashViewFilter[]
        {
            new HashAlgoFilter(),           // 0
            new CmpResultFilter(),          // 1
            new HashingTaskResultFilter(),  // 2
            new SerialNumberFilter(),       // 3
            new HashStringFilter(),         // 4
            // 这个筛选器因其特殊性需最后应用，否则结果不正确
            new EqualHashByteFilter(),      // 5
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

        public bool RefreshEnabled
        {
            get
            {
                return this._refreshEnabled;
            }
            set
            {
                this.SetPropNotify(ref this._refreshEnabled, value);
            }
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
            this.RefreshEnabled = false;
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
                }
                if (filteringShouldBeApplied)
                {
                    foreach (HashViewFilter filter in this.HashModelFilters)
                    {
                        if (filter.Selected)
                        {
                            filter.FilterObjects(MainWndViewModel.HashViewModels);
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
            this.RefreshEnabled = true;
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
