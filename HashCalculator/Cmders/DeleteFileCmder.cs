using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.Others;
using HashCalculator.ViewModels.Pages;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Windows;
using Wpfctrls = Wpf.Ui.Controls;

namespace HashCalculator
{
    internal class DeleteFileCmder : AbsHashesCmder
    {
        private RelayCommand moveToRecycleBinCmd;
        private RelayCommand deleteFileDirectlyCmd;

        public override ContentControl UserInterface { get; }

        public override string Display => "删除操作目标所指的文件";

        public override string Description => "直接删除操作目标所指的文件或移动到回收站，通常使用〈相同哈希值〉筛选器进行文件筛选后再使用此功能。";

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public DeleteFileCmder() : this(HashModelStore.HashViewModels)
        {
        }

        public DeleteFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this.UserInterface = new DeleteFileCmderCtrl(this);
        }

        private async void DeleteOrMoveToRecycleBin(bool toRecyclebin)
        {
            if (Settings.Current.IsFiltersAndCmdersIdle &&
                this.RefModels is RangeObservableCollection<HashViewModel> hashViewModels)
            {
                Settings.Current.IsFiltersAndCmdersIdle = false;
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    string promptInfo = toRecyclebin ? "确定把操作目标所指的文件移动到回收站吗？"
                        : "确定直接删除操作目标所指的文件吗？";
                    if (NotificationSender.ShowMessageBox(
                        MainWindow.Current,
                        "警告",
                        promptInfo,
                        closeButtonText: "取消",
                        primaryButtonText: "确定") == Wpfctrls.ContentDialogResult.Primary)
                    {
                        if (this.CheckIfUsingDistinctFilesFilter &&
                            !hashViewModels.Where(i => i.Matched).All(i => i.FileIndex != null))
                        {
                            if (NotificationSender.ShowMessageBox(
                                MainWindow.Current,
                                "提示",
                                "没有应用〈有效的文件〉筛选器，要继续操作吗？",
                                closeButtonText: "否",
                                primaryButtonText: "是") != Wpfctrls.ContentDialogResult.Primary)
                            {
                                goto FinishingTouches;
                            }
                        }
                        HashViewModel[] targets = hashViewModels.Where(i => i.IsExecutionTarget).ToArray();
                        ProgressWindowModel progress = new ProgressWindowModel()
                        {
                            IsCancelled = true,
                            TotalCount = targets.Length,
                            SubProgressVisibility = Visibility.Collapsed,
                            TotalProgressVisibility = Visibility.Collapsed,
                            WindowTitle = "正在删除...",
                            TotalString = "文件数量多的情况下耗时较长，请耐心等候...",
                        };
                        ProgressWindow progressWindow = new ProgressWindow(progress)
                        {
                            Owner = MainWindow.Current,
                        };
                        foreach (HashViewModel model in targets)
                        {
                            model.ShutdownModelWait();
                        }
                        hashViewModels.RemoveItems(targets);
                        Task<string> deleteFilesTask = Task.Run(() =>
                        {
                            try
                            {
                                if (!toRecyclebin)
                                {
                                    List<string> fileNameList = new List<string>();
                                    foreach (HashViewModel model in targets)
                                    {
                                        try
                                        {
                                            model.Information.Delete();
                                        }
                                        catch (Exception)
                                        {
                                            fileNameList.Add(model.FileName);
                                        }
                                    }
                                    if (fileNameList.Count != 0)
                                    {
                                        return "以下文件删除失败：\n" + '\n'.Join(fileNameList);
                                    }
                                    return default(string);
                                }
                                else
                                {
                                    string pathsInOneString = '\0'.Join(
                                        targets.Select(i => i.Information.FullName));
                                    if (!CommonUtils.SendToRecycleBin(MainWindow.WndHandle, pathsInOneString,
                                        Settings.Current.MoveFilesToRecycleBinSilently))
                                    {
                                        return "移动文件到回收站失败，可能部分文件未移动！";
                                    }
                                }
                                return default(string);
                            }
                            catch (Exception ex)
                            {
                                return $"删除文件或移动文件到回收站的过程出现异常：{ex.Message}";
                            }
                            finally
                            {
                                progress.AutoClose = true;
                                Synchronization.UI.Invoke(() =>
                                {
                                    progressWindow.DialogResult = false;
                                });
                            }
                        });
                        progressWindow.ShowDialog();
                        string exceptionMessage = await deleteFilesTask;
                        if (!string.IsNullOrEmpty(exceptionMessage))
                        {
                            NotificationSender.ShowMessageBox(MainWindow.Current, "错误", exceptionMessage);
                        }
                        HomeViewModel.Current.GenerateFileHashCheckReport();
                    }
                }
                else
                {
                    NotificationSender.ShowMessageBox(
                        MainWindow.Current, "提示", "没有找到任何操作目标，请选择操作目标！");
                }
            FinishingTouches:
                Settings.Current.IsFiltersAndCmdersIdle = true;
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
                this.moveToRecycleBinCmd ??= new RelayCommand(this.MoveToRecycleBinAction);
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
                this.deleteFileDirectlyCmd ??= new RelayCommand(this.DeleteFileDirectlyAction);
                return this.deleteFileDirectlyCmd;
            }
        }
    }
}
