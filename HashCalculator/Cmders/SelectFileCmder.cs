using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HashCalculator
{
    internal class SelectFileCmder : HashViewCmder
    {
        private RelayCommand selectAllModelsCmd = null;
        private RelayCommand deselectAllModelsCmd = null;
        private RelayCommand reverseSelectModelsCmd = null;
        private RelayCommand selectGroupingsCmd = null;
        private RelayCommand cancelSelectionCmd = null;

        public override string Display => "选择操作对象";

        public override string Description => "提供不同的快速选择方法来选择行，供其他操作使用";

        public SelectFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public SelectFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
        }

        public override void Reset()
        {
            this.DeselectAllModelsAction(null);
            Settings.Current.ShowExecutionTargetColumn = false;
        }

        private void SelectAllModelsAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = model.Matched;
                }
                Settings.Current.ShowExecutionTargetColumn = true;
            }
        }

        public ICommand SelectAllModelsCmd
        {
            get
            {
                if (this.selectAllModelsCmd == null)
                {
                    this.selectAllModelsCmd = new RelayCommand(this.SelectAllModelsAction);
                }
                return this.selectAllModelsCmd;
            }
        }

        private void DeselectAllModelsAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
                Settings.Current.ShowExecutionTargetColumn = true;
            }
        }

        public ICommand DeselectAllModelsCmd
        {
            get
            {
                if (this.deselectAllModelsCmd == null)
                {
                    this.deselectAllModelsCmd = new RelayCommand(this.DeselectAllModelsAction);
                }
                return this.deselectAllModelsCmd;
            }
        }

        private void ReverseSelectModelsAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = model.Matched && !model.IsExecutionTarget;
                }
                Settings.Current.ShowExecutionTargetColumn = true;
            }
        }

        public ICommand ReverseSelectModelsCmd
        {
            get
            {
                if (this.reverseSelectModelsCmd == null)
                {
                    this.reverseSelectModelsCmd = new RelayCommand(this.ReverseSelectModelsAction);
                }
                return this.reverseSelectModelsCmd;
            }
        }

        private void SelectGroupingsAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
                IEnumerable<IGrouping<ComparableColor, HashViewModel>> byGroupId =
                    models.Where(i => i.Matched && i.GroupId != null).GroupBy(i => i.GroupId);
                foreach (IGrouping<ComparableColor, HashViewModel> group in byGroupId)
                {
                    foreach (HashViewModel model in group.Skip(1))
                    {
                        model.IsExecutionTarget = true;
                    }
                }
                Settings.Current.ShowExecutionTargetColumn = true;
            }
        }

        public ICommand SelectGroupingsCmd
        {
            get
            {
                if (this.selectGroupingsCmd == null)
                {
                    this.selectGroupingsCmd = new RelayCommand(this.SelectGroupingsAction);
                }
                return this.selectGroupingsCmd;
            }
        }

        public ICommand CancelSelectionCmd
        {
            get
            {
                if (this.cancelSelectionCmd == null)
                {
                    this.cancelSelectionCmd = new RelayCommand(@object => { this.Reset(); });
                }
                return this.cancelSelectionCmd;
            }
        }
    }
}
