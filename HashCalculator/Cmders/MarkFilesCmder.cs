using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator
{
    internal class MarkFilesCmder : AbsHashesCmder
    {
        private RelayCommand selectFolderCmd;
        private RelayCommand changeFilesHashCmd;
        private string folderUsedToSaveFiles;

        public override ContentControl UserInterface { get; }

        public override string Display => "生成有哈希标记的新文件";

        public override string Description => "生成带哈希标记和随机数据的新文件用于避过哈希检测。\n当需要使用文件时需要先从带标记的文件中还原出原文件。";

        public string FolderUsedToSaveFiles
        {
            get => this.folderUsedToSaveFiles;
            set => this.SetPropNotify(ref this.folderUsedToSaveFiles, value);
        }

        public bool UseSenseFreeModifications { get; set; } = false;

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public MarkFilesCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public MarkFilesCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this.UserInterface = new MarkFilesCmderCtrl(this);
        }

        private void SelectFolderAction(object param)
        {
            CommonOpenFileDialog folderOpen = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = Settings.Current.LastUsedPath,
                EnsureValidNames = true,
            };
            if (folderOpen.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.FolderUsedToSaveFiles = folderOpen.FileName;
                Settings.Current.LastUsedPath = folderOpen.FileName;
            }
        }

        public ICommand SelectFolderCmd
        {
            get
            {
                if (this.selectFolderCmd == null)
                {
                    this.selectFolderCmd = new RelayCommand(this.SelectFolderAction);
                }
                return this.selectFolderCmd;
            }
        }

        private async Task<string> ChangeFilesHash(IEnumerable<HashViewModel> models,
            DoubleProgressWindow doubleProgressWindow, DoubleProgressModel doubleProgressModel)
        {
            try
            {
                IEnumerable<HashViewModel> succeededModels = models.Where(
                    i => i.Result == HashResult.Succeeded);
                doubleProgressModel.FilesCount = succeededModels.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in succeededModels)
                    {
                        doubleProgressModel.ProgressValue = 0.0;
                        doubleProgressModel.CurFileName = model.FileName;
                        try
                        {
                            string newExt;
                            string oldExt = Path.GetExtension(model.FileName);
                            string noExtName = Path.GetFileNameWithoutExtension(model.FileName);
                            if (this.UseSenseFreeModifications && !string.IsNullOrEmpty(oldExt) && (
                                oldExt.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                oldExt.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                oldExt.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
                            {

                                newExt = $".hcm{oldExt}";
                            }
                            else
                            {
                                newExt = $"{oldExt}.hcm";
                            }
                            int duplicate = 0;
                            string newFilePath;
                            string newFileName = $"{noExtName}{newExt}";
                            while (true)
                            {
                                newFilePath = Path.Combine(this.FolderUsedToSaveFiles, newFileName);
                                if (File.Exists(newFilePath))
                                {
                                    newFileName = $"{noExtName}_{++duplicate}{newExt}";
                                    continue;
                                }
                                break;
                            }
                            using (Stream oldStream = model.FileInfo.OpenRead())
                            using (HcFileHelper hcFileHelper = new HcFileHelper(oldStream))
                            using (Stream newStream = File.OpenWrite(newFilePath))
                            {
                                AlgoInOutModel currentAlgoModel = model.CurrentInOutModel;
                                hcFileHelper.GenerateHcFile(
                                    newStream, currentAlgoModel, this.UseSenseFreeModifications, doubleProgressModel);
                            }
                        }
                        catch (Exception) { }
                        doubleProgressModel.ProcessedCount += 1;
                        if (doubleProgressModel.TokenSrc.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                });
                return default(string);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            finally
            {
                doubleProgressModel.AutoClose = true;
                doubleProgressWindow.DialogResult = false;
            }
        }

        private async void ChangeFilesHashAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (string.IsNullOrEmpty(this.FolderUsedToSaveFiles) ||
                    !Path.IsPathRooted(this.FolderUsedToSaveFiles))
                {
                    MessageBox.Show(MainWindow.This, "请输入生成的新文件的保存目录的完整路径！", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    goto FinishingTouches;
                }
                if (this.CheckIfUsingDistinctFilesFilter && !hashViewModels.Where(
                    i => i.Matched).All(i => i.FileIndex != null))
                {
                    if (MessageBox.Show(MainWindow.This, "并非所有筛选出的行都经过【有效文件】的筛选，继续吗？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        goto FinishingTouches;
                    }
                }
                if (!Directory.Exists(this.FolderUsedToSaveFiles))
                {
                    try
                    {
                        Directory.CreateDirectory(this.FolderUsedToSaveFiles);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(MainWindow.This, "用于保存生成的新文件的目录不存在且创建失败！", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        goto FinishingTouches;
                    }
                }
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    DoubleProgressModel progressModel = new DoubleProgressModel();
                    DoubleProgressWindow progressWindow = new DoubleProgressWindow(progressModel)
                    {
                        Owner = MainWindow.This
                    };
                    Task<string> changeHashTask = this.ChangeFilesHash(hashViewModels, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await changeHashTask;
                    if (!string.IsNullOrEmpty(exceptionMessage))
                    {
                        MessageBox.Show(MainWindow.This, $"出现异常导致过程中断：{exceptionMessage}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        goto FinishingTouches;
                    }
                }
                else
                {
                    MessageBox.Show(MainWindow.This, "没有找到任何操作目标，请刷新筛选或手动勾选操作目标！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            FinishingTouches:
                Settings.Current.FilterOrCmderEnabled = true;
                Settings.Current.ShowExecutionTargetColumn = false;
            }
        }

        public ICommand ChangeFilesHashCmd
        {
            get
            {
                if (this.changeFilesHashCmd == null)
                {
                    this.changeFilesHashCmd = new RelayCommand(this.ChangeFilesHashAction);
                }
                return this.changeFilesHashCmd;
            }
        }
    }
}
