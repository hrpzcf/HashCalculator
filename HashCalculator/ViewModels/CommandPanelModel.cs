using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HashCalculator
{
    internal class CommandPanelModel : NotifiableModel
    {
        private bool _refreshEnabled = true;
        private readonly PropertyGroupDescription groupDescription =
            new PropertyGroupDescription(nameof(HashViewModel.GroupId));
        private RelayCommand refreshViewCmd;
        private RelayCommand filterChangedCmd;

        private List<HashViewFilter<HashViewModel>> HashModelFilters { get; } =
            new List<HashViewFilter<HashViewModel>>();

        private List<HashViewFilter<IEnumerable<HashViewModel>>> HashModelIEnumFilters { get; } =
            new List<HashViewFilter<IEnumerable<HashViewModel>>>();

        public void ClearFiltersAndRefresh()
        {
            foreach (HashViewFilter<HashViewModel> filter1 in this.HashModelFilters)
            {
                filter1.Finish();
            }
            foreach (HashViewFilter<IEnumerable<HashViewModel>> filter2 in this.HashModelIEnumFilters)
            {
                filter2.Finish();
            }
            this.HashModelFilters.Clear();
            this.HashModelIEnumFilters.Clear();
            this.RefreshViewAction(null);
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
            if (param is HashViewFilter<HashViewModel> filter)
            {
                if (!filter.Selected)
                {
                    this.HashModelFilters.Remove(filter);
                }
                else if (!this.HashModelFilters.Contains(filter))
                {
                    this.HashModelFilters.Add(filter);
                }
            }
            else if (param is HashViewFilter<IEnumerable<HashViewModel>> ienumFilter)
            {
                if (!ienumFilter.Selected)
                {
                    this.HashModelIEnumFilters.Remove(ienumFilter);
                }
                else if (!this.HashModelIEnumFilters.Contains(ienumFilter))
                {
                    this.HashModelIEnumFilters.Add(ienumFilter);
                }
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
            await Task.Run(() =>
            {
                foreach (HashViewModel model in MainWndViewModel.HashViewModels)
                {
                    model.GroupId = default(ComparableColor);
                    model.Matched = true;
                    foreach (HashViewFilter<HashViewModel> filter in this.HashModelFilters)
                    {
                        filter.SetFilterTags(model);
                    }
                }
                if (this.HashModelIEnumFilters.Any())
                {
                    foreach (HashViewFilter<IEnumerable<HashViewModel>> filter in this.HashModelIEnumFilters)
                    {
                        filter.SetFilterTags(MainWndViewModel.HashViewModels);
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool refreshed = false;
                    if (this.HashModelIEnumFilters.Any())
                    {
                        MainWndViewModel.HashViewModelsView.GroupDescriptions.Add(this.groupDescription);
                        refreshed = true;
                    }
                    else
                    {
                        refreshed = MainWndViewModel.HashViewModelsView.GroupDescriptions.Remove(this.groupDescription);
                    }
                    if (!refreshed)
                    {
                        MainWndViewModel.HashViewModelsView.Refresh();
                    }
                });
            });
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
