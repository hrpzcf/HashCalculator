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
        private RelayCommand generateTaggedFilesCmd;
        private string directoryUsedToSaveFiles;
        private bool saveToOriginDirectory = true;

        private static readonly SizeDelegates delegates = new SizeDelegates()
        {
            GetWindowWidth = () => Settings.Current.MarkFilesProgressWidth,
            SetWindowWidth = width => Settings.Current.MarkFilesProgressWidth = width,
            GetWindowHeight = () => Settings.Current.MarkFilesProgressHeight,
            SetWindowHeight = height => Settings.Current.MarkFilesProgressHeight = height,
        };

        public override ContentControl UserInterface { get; }

        public override string Display => "生成带哈希标记的新文件";

        public override string Description => "用原文件哈希值、随机数据和原文件组合成 .hcm 新文件用于避过一些网络平台的哈希检测。\n当要使用原文件时需要先从带标记的文件中还原 (无痕标记的 PNG/JPEG 文件可以不用还原)。";

        public bool SaveToOriginDirectory
        {
            get => this.saveToOriginDirectory;
            set => this.SetPropNotify(ref this.saveToOriginDirectory, value);
        }

        public string DirectoryUsedToSaveFiles
        {
            get => this.directoryUsedToSaveFiles;
            set => this.SetPropNotify(ref this.directoryUsedToSaveFiles, value);
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
                this.DirectoryUsedToSaveFiles = folderOpen.FileName;
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

        private async Task<string> GenerateTaggedFiles(IEnumerable<HashViewModel> models,
            DoubleProgressWindow doubleProgressWindow, DoubleProgressModel doubleProgressModel)
        {
            try
            {
                IEnumerable<HashViewModel> succeededModels = models.Where(
                    i => i.Result == HashResult.Succeeded);
                doubleProgressModel.TotalCount = succeededModels.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in succeededModels)
                    {
                        doubleProgressModel.CurrentValue = 0.0;
                        doubleProgressModel.CurrentString = model.FileName;
                        try
                        {
                            using (FileStream fileStream = model.FileInfo.OpenRead())
                            using (FileDataHelper fileDataHelper = new FileDataHelper(fileStream))
                            {
                                if (!fileDataHelper.TryGetFileDataInfo(out FileDataInfo info) || info.IsTagged)
                                {
                                    goto NextRound;
                                }
                                string oldExtension = Path.GetExtension(model.FileName);
                                string newNameNoExt = Path.GetFileNameWithoutExtension(model.FileName);
                                string newExt = this.UseSenseFreeModifications &&
                                    !string.IsNullOrEmpty(oldExtension) && (
                                        oldExtension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                        oldExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                        oldExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                                    ) ? $".hcm{oldExtension}" : $"{oldExtension}.hcm";
                                string directoryToSaveFile = this.SaveToOriginDirectory ?
                                    model.FileInfo.DirectoryName : this.DirectoryUsedToSaveFiles;
                                int duplicate = -1;
                                string newFilePath;
                                do
                                {
                                    string newFileName = ++duplicate == 0 ?
                                        $"{newNameNoExt}{newExt}" : $"{newNameNoExt}_{duplicate}{newExt}";
                                    newFilePath = Path.Combine(directoryToSaveFile, newFileName);
                                } while (File.Exists(newFilePath));
                                using (FileStream newFileStream = File.Create(newFilePath))
                                {
                                    fileDataHelper.GenerateTaggedFile(newFileStream, info, model.CurrentInOutModel,
                                        this.UseSenseFreeModifications, doubleProgressModel);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    NextRound:
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

        private async void GenerateTaggedFilesAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (!this.SaveToOriginDirectory)
                {
                    if (string.IsNullOrEmpty(this.DirectoryUsedToSaveFiles) ||
                        !Path.IsPathRooted(this.DirectoryUsedToSaveFiles))
                    {
                        MessageBox.Show(MainWindow.This, "请输入生成的新文件的保存目录的完整路径！", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        goto FinishingTouches;
                    }
                    if (!Directory.Exists(this.DirectoryUsedToSaveFiles))
                    {
                        try
                        {
                            Directory.CreateDirectory(this.DirectoryUsedToSaveFiles);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(MainWindow.This, "用于保存生成的新文件的目录不存在且创建失败！", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            goto FinishingTouches;
                        }
                    }
                }
                if (this.CheckIfUsingDistinctFilesFilter && !hashViewModels.Where(
                    i => i.Matched).All(i => i.FileIndex != null))
                {
                    if (MessageBox.Show(MainWindow.This, "没有应用【有效的文件】筛选器，要继续操作吗？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        goto FinishingTouches;
                    }
                }
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    IEnumerable<HashViewModel> targets = hashViewModels.Where(i => i.IsExecutionTarget);
                    DoubleProgressModel progressModel = new DoubleProgressModel(delegates);
                    DoubleProgressWindow progressWindow = new DoubleProgressWindow(progressModel)
                    {
                        Owner = MainWindow.This
                    };
                    Task<string> genTaggedFilesTask = this.GenerateTaggedFiles(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await genTaggedFilesTask;
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

        public ICommand GenerateTaggedFilesCmd
        {
            get
            {
                if (this.generateTaggedFilesCmd == null)
                {
                    this.generateTaggedFilesCmd = new RelayCommand(this.GenerateTaggedFilesAction);
                }
                return this.generateTaggedFilesCmd;
            }
        }
    }
}
