using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    internal class DeleteFileCmder : AbsHashesCmder
    {
        private RelayCommand moveToRecycleBinCmd;
        private RelayCommand deleteFileDirectlyCmd;

        public override ContentControl UserInterface { get; }

        public override string Display => "删除操作目标所指的文件";

        public override string Description => "直接删除操作目标所指的文件或移动到回收站；\n通常使用【相同哈希值】筛选器进行文件筛选后再使用此功能。";

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public DeleteFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public DeleteFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this.UserInterface = new DeleteFileCmderCtrl(this);
        }

        private async void DeleteOrMoveToRecycleBin(bool toRecyclebin)
        {
            if (Settings.Current.FilterAndCmderEnabled &&
                this.RefModels is ObservableCollection<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterAndCmderEnabled = false;
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    string promptInfo = toRecyclebin ? "确定把操作目标所指的文件移动到回收站吗？" :
                        "确定直接删除操作目标所指的文件吗？";
                    if (MessageBox.Show(MainWindow.Current, promptInfo, "警告", MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        if (this.CheckIfUsingDistinctFilesFilter &&
                            !hashViewModels.Where(i => i.Matched).All(i => i.FileIndex != null))
                        {
                            if (MessageBox.Show(MainWindow.Current, "没有应用【有效的文件】筛选器，要继续操作吗？", "提示",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            {
                                goto FinishingTouches;
                            }
                        }
                        HashViewModel[] targets = hashViewModels.Where(i => i.IsExecutionTarget).ToArray();
                        foreach (HashViewModel model in targets)
                        {
                            model.ShutdownModelWait();
                            hashViewModels.Remove(model);
                        }
                        await Task.Run(() =>
                        {
                            if (!toRecyclebin)
                            {
                                foreach (HashViewModel model in targets)
                                {
                                    try { model.Information.Delete(); } catch (Exception) { }
                                }
                            }
                            else
                            {
                                string pathsInOneString = '\0'.Join(targets.Select(i => i.Information.FullName));
                                CommonUtils.SendToRecycleBin(MainWindow.WndHandle, pathsInOneString);
                            }
                        });
                        MainWndViewModel.Current.GenerateFileHashCheckReport();
                    }
                }
                else
                {
                    MessageBox.Show(MainWindow.Current, "没有找到任何操作目标，请刷新筛选或手动勾选要删除的对象", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            FinishingTouches:
                Settings.Current.FilterAndCmderEnabled = true;
                Settings.Current.IsMainRowSelectedByCheckBox = false;
            }
        }

        private void MoveToRecycleBinAction(object param)
        {
            this.DeleteOrMoveToRecycleBin(true);
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
            this.DeleteOrMoveToRecycleBin(false);
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
