using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DeleteFileCmder : AbsHashesCmder
    {
        private RelayCommand moveToRecycleBinCmd;
        private RelayCommand deleteFileDirectlyCmd;

        public override string Display => "删除操作对象所指文件";

        public override string Description => "直接删除操作对象所指的文件或移动到回收站\n通常使用【相同哈希值】筛选器进行文件筛选后再使用此功能";

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public DeleteFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public DeleteFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
        }

        private void DeleteMoveToRecycleBin(bool permanently)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is ObservableCollection<HashViewModel> obsModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (obsModels.Any(i => i.IsExecutionTarget))
                {
                    string promptInfo = permanently ? "确定直接删除操作对象所指的文件吗？" :
                        "确定把操作对象所指的文件移动到回收站吗？";
                    if (MessageBox.Show(MainWindow.This, promptInfo, "警告", MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        if (this.CheckIfUsingDistinctFilesFilter &&
                            !obsModels.Where(i => i.Matched).All(i => i.FileIndex != null))
                        {
                            if (MessageBox.Show(MainWindow.This, "并非所有行都经过【有效文件】的筛选，继续吗？", "提示",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            {
                                goto FinishingTouches;
                            }
                        }
                        HashViewModel[] hashViewModels = new HashViewModel[obsModels.Count];
                        obsModels.CopyTo(hashViewModels, 0);
                        foreach (HashViewModel model in hashViewModels)
                        {
                            if (model.IsExecutionTarget)
                            {
                                try
                                {
                                    if (permanently)
                                    {
                                        model.ModelShutdownEvent += m => { m.FileInfo.Delete(); };
                                    }
                                    else
                                    {
                                        model.ModelShutdownEvent += m =>
                                        {
                                            CommonUtils.SendToRecycleBin(MainWindow.WndHandle, m.FileInfo.FullName);
                                        };
                                    }
                                    model.ShutdownModel();
                                    obsModels.Remove(model);
                                    continue;
                                }
                                catch (Exception) { }
                            }
                            model.IsExecutionTarget = false;
                        }
                        MainWndViewModel.CurrentModel.GenerateFileHashCheckReport();
                    }
                }
                else
                {
                    MessageBox.Show(MainWindow.This, "没有找到任何操作对象，请刷新筛选或手动勾选要删除的对象", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            FinishingTouches:
                Settings.Current.FilterOrCmderEnabled = true;
                Settings.Current.ShowExecutionTargetColumn = false;
            }
        }

        private void MoveToRecycleBinAction(object param)
        {
            this.DeleteMoveToRecycleBin(false);
        }

        public ICommand MoveToRecycleBinCmd
        {
            get
            {
                if (this.moveToRecycleBinCmd == null)
                {
                    this.moveToRecycleBinCmd = new RelayCommand(this.MoveToRecycleBinAction);
                }
                return this.moveToRecycleBinCmd;
            }
        }

        private void DeleteFileDirectlyAction(object param)
        {
            this.DeleteMoveToRecycleBin(true);
        }

        public ICommand DeleteFileDirectlyCmd
        {
            get
            {
                if (this.deleteFileDirectlyCmd == null)
                {
                    this.deleteFileDirectlyCmd = new RelayCommand(this.DeleteFileDirectlyAction);
                }
                return this.deleteFileDirectlyCmd;
            }
        }
    }
}
