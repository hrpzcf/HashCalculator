using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DelEqualHashFileCmder : HashViewCmder
    {
        private RelayCommand cancelExecutionCmd;
        private ControlItem cancelExecutionCmdCtrl;
        private RelayCommand executeCommandCmd;
        private ControlItem executeCommandCmdCtrl;
        private RelayCommand prepareExecutionTargetCmd;
        private ControlItem prepareExecutionTargetCmdCtrl;

        public override string Display => "删除指定文件";

        public override string Description => "把筛选出来的结果中已勾选的文件移动到回收站";

        public DelEqualHashFileCmder() : base(MainWndViewModel.HashViewModels)
        {
        }

        public DelEqualHashFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
        }

        public override void Reset()
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
            }
            Settings.Current.NoExecutionTargetColumn = true;
        }

        private ICommand CancelExecutionCmd
        {
            get
            {
                if (this.cancelExecutionCmd == null)
                {
                    this.cancelExecutionCmd = new RelayCommand(o => { this.Reset(); });
                }
                return this.cancelExecutionCmd;
            }
        }

        public ControlItem CancelExecutionCmdCtrl
        {
            get
            {
                if (this.cancelExecutionCmdCtrl == null)
                {
                    this.cancelExecutionCmdCtrl = new ControlItem("取消", this.CancelExecutionCmd);
                }
                return this.cancelExecutionCmdCtrl;
            }
        }

        private void ExecuteCommandAction(object param)
        {
            if (!Settings.Current.NoExecutionTargetColumn &&
                this.RefModels is ObservableCollection<HashViewModel> obsModels)
            {
                if (obsModels.Any(i => i.IsExecutionTarget))
                {
                    if (MessageBox.Show(MainWindow.This, "确定把【删除】列已勾选的文件移动到回收站吗？", "警告",
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        HashViewModel[] hashViewModels = new HashViewModel[obsModels.Count];
                        obsModels.CopyTo(hashViewModels, 0);
                        foreach (HashViewModel model in hashViewModels)
                        {
                            if (model.IsExecutionTarget)
                            {
                                model.ModelShutdownEvent += m =>
                                {
                                    CommonUtils.SendToRecycleBin(MainWindow.WndHandle, m.FileInfo.FullName);
                                };
                                model.ShutdownModel();
                                obsModels.Remove(model);
                            }
                        }
                        MainWndViewModel.CurrentModel.GenerateVerificationReport();
                    }
                }
                else
                {
                    MessageBox.Show(MainWindow.This, "没有找到任何已勾选的行，请刷新筛选或手动勾选要删除的行", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                this.Reset();
            }
        }

        private ICommand ExecuteCommandCmd
        {
            get
            {
                if (this.executeCommandCmd == null)
                {
                    this.executeCommandCmd = new RelayCommand(this.ExecuteCommandAction);
                }
                return this.executeCommandCmd;
            }
        }

        public ControlItem ExecuteCommandCmdCtrl
        {
            get
            {
                if (this.executeCommandCmdCtrl == null)
                {
                    this.executeCommandCmdCtrl = new ControlItem("删除", this.ExecuteCommandCmd);
                }
                return this.executeCommandCmdCtrl;
            }
        }

        private void PrepareExecutionTargetAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                IEnumerable<IGrouping<ComparableColor, HashViewModel>> byGroupId =
                    models.Where(i => i.Matched && i.GroupId != null).GroupBy(i => i.GroupId);
                foreach (IGrouping<ComparableColor, HashViewModel> group in byGroupId)
                {
                    foreach (HashViewModel model in group.Skip(1))
                    {
                        model.IsExecutionTarget = true;
                    }
                }
                Settings.Current.NoExecutionTargetColumn = false;
            }
            else
            {
                MessageBox.Show(MainWindow.This, "没有找到要执行的目标，请刷新筛选", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private ICommand PrepareExecutionTargetCmd
        {
            get
            {
                if (this.prepareExecutionTargetCmd == null)
                {
                    this.prepareExecutionTargetCmd = new RelayCommand(this.PrepareExecutionTargetAction);
                }
                return this.prepareExecutionTargetCmd;
            }
        }

        public ControlItem PrepareExecutionTargetCmdCtrl
        {
            get
            {
                if (this.prepareExecutionTargetCmdCtrl == null)
                {
                    this.prepareExecutionTargetCmdCtrl = new ControlItem("选择", this.PrepareExecutionTargetCmd);
                }
                return this.prepareExecutionTargetCmdCtrl;
            }
        }
    }
}
