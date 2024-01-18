using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HashCalculator
{
    internal class SelectTargetsCmder : AbsHashesCmder
    {
        private RelayCommand cancelSelectionCmd = null;
        private RelayCommand selectSameHashGroupCmd = null;
        private RelayCommand selectSameFolderGroupCmd = null;
        private RelayCommand selectHybridGroupsCmd = null;
        private RelayCommand selectAllModelsCmd = null;
        private RelayCommand deselectAllModelsCmd = null;
        private RelayCommand reverseSelectModelsCmd = null;

        private ICollectionView BoundDataGridView { get; }

        public override ContentControl UserInterface { get; }

        public override string Display => "选择操作目标（在主窗口表格中显示【操作目标】列并勾选相关行）";

        public override string Description => "提供不同的快速选择方法来选择不同的行以用作其他操作器的目标。";

        public SelectTargetsCmder(IEnumerable<HashViewModel> models, ICollectionView view) : base(models)
        {
            this.BoundDataGridView = view;
            this.UserInterface = new SelectTargetsCmderCtrl(this);
        }

        public SelectTargetsCmder() : this(MainWndViewModel.HashViewModels, MainWndViewModel.HashViewModelsView)
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

        private void SelectSameHashGroupAction(object param)
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

        public ICommand SelectSameHashGroupCmd
        {
            get
            {
                if (this.selectSameHashGroupCmd == null)
                {
                    this.selectSameHashGroupCmd = new RelayCommand(this.SelectSameHashGroupAction);
                }
                return this.selectSameHashGroupCmd;
            }
        }

        private void SelectSameFolderGroupAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
                IEnumerable<IGrouping<ComparableColor, HashViewModel>> byGroupId =
                    models.Where(i => i.Matched && i.FdGroupId != null).GroupBy(i => i.FdGroupId);
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

        public ICommand SelectSameFolderGroupCmd
        {
            get
            {
                if (this.selectSameFolderGroupCmd == null)
                {
                    this.selectSameFolderGroupCmd = new RelayCommand(this.SelectSameFolderGroupAction);
                }
                return this.selectSameFolderGroupCmd;
            }
        }

        private void CheckCollectionViewGroupItems(IEnumerable<object> groups)
        {
            if (groups != null)
            {
                foreach (CollectionViewGroup group in groups.Cast<CollectionViewGroup>())
                {
                    if (group.IsBottomLevel)
                    {
                        foreach (HashViewModel model in group.Items.Skip(1).Cast<HashViewModel>())
                        {
                            model.IsExecutionTarget = true;
                        }
                    }
                    else
                    {
                        this.CheckCollectionViewGroupItems(group.Items);
                    }
                }
            }
        }

        private void SelectHybridGroupsAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
                this.CheckCollectionViewGroupItems(this.BoundDataGridView.Groups);
                Settings.Current.ShowExecutionTargetColumn = true;
            }
        }

        public ICommand SelectHybridGroupsCmd
        {
            get
            {
                if (this.selectHybridGroupsCmd == null)
                {
                    this.selectHybridGroupsCmd = new RelayCommand(this.SelectHybridGroupsAction);
                }
                return this.selectHybridGroupsCmd;
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
