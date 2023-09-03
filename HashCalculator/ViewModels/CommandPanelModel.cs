using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class CommandPanelModel : NotifiableModel
    {
        private bool _refreshEnabled = true;
        private RelayCommand refreshViewCmd;
        private RelayCommand selectorChangedCmd;

        public List<HashSelector<HashViewModel>> HashSelectors { get; } =
            new List<HashSelector<HashViewModel>>();

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
            if (param is HashSelector<HashViewModel> selector)
            {
                if (!selector.Selected)
                {
                    this.HashSelectors.Remove(selector);
                }
                else if (!this.HashSelectors.Contains(selector))
                {
                    this.HashSelectors.Add(selector);
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

        public async void RefreshViewAction(object param)
        {
            this.RefreshEnabled = false;
            await Task.Run(() =>
            {
                foreach (HashViewModel model in MainWndViewModel.HashViewModels)
                {
                    model.Matched = true;
                    foreach (HashSelector<HashViewModel> selector in this.HashSelectors)
                    {
                        selector.SetFilterTags(model);
                    }
                }
                Application.Current.Dispatcher.Invoke(() => { MainWndViewModel.HashViewModelsViewSrc.View.Refresh(); });
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
