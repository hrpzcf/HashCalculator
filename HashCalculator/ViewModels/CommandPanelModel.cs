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
        private RelayCommand selectorChangedCmd;

        private List<HashViewFilter<HashViewModel>> HashModelSelectors { get; } =
            new List<HashViewFilter<HashViewModel>>();

        private List<HashViewFilter<IEnumerable<HashViewModel>>> HashModelIEnumSelectors { get; } =
            new List<HashViewFilter<IEnumerable<HashViewModel>>>();

        public void ClearSelectorsAndRefresh()
        {
            foreach (HashViewFilter<HashViewModel> selector1 in this.HashModelSelectors)
            {
                selector1.Finish();
            }
            foreach (HashViewFilter<IEnumerable<HashViewModel>> selector2 in this.HashModelIEnumSelectors)
            {
                selector2.Finish();
            }
            this.HashModelSelectors.Clear();
            this.HashModelIEnumSelectors.Clear();
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

        private void SelectorChangedAction(object param)
        {
            if (param is HashViewFilter<HashViewModel> selector)
            {
                if (!selector.Selected)
                {
                    this.HashModelSelectors.Remove(selector);
                }
                else if (!this.HashModelSelectors.Contains(selector))
                {
                    this.HashModelSelectors.Add(selector);
                }
            }
            else if (param is HashViewFilter<IEnumerable<HashViewModel>> ienumSelector)
            {
                if (!ienumSelector.Selected)
                {
                    this.HashModelIEnumSelectors.Remove(ienumSelector);
                }
                else if (!this.HashModelIEnumSelectors.Contains(ienumSelector))
                {
                    this.HashModelIEnumSelectors.Add(ienumSelector);
                }
            }
        }

        public ICommand SelectorChangedCmd
        {
            get
            {
                if (this.selectorChangedCmd == null)
                {
                    this.selectorChangedCmd = new RelayCommand(this.SelectorChangedAction);
                }
                return this.selectorChangedCmd;
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
                    foreach (HashViewFilter<HashViewModel> selector in this.HashModelSelectors)
                    {
                        selector.SetFilterTags(model);
                    }
                }
                if (this.HashModelIEnumSelectors.Any())
                {
                    foreach (HashViewFilter<IEnumerable<HashViewModel>> selector in this.HashModelIEnumSelectors)
                    {
                        selector.SetFilterTags(MainWndViewModel.HashViewModels);
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool refreshed = false;
                    if (this.HashModelIEnumSelectors.Any())
                    {
                        MainWndViewModel.HashViewModelsViewSrc.View.GroupDescriptions.Add(this.groupDescription);
                        refreshed = true;
                    }
                    else
                    {
                        refreshed = MainWndViewModel.HashViewModelsViewSrc.View.GroupDescriptions.Remove(this.groupDescription);
                    }
                    if (!refreshed)
                    {
                        MainWndViewModel.HashViewModelsViewSrc.View.Refresh();
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
