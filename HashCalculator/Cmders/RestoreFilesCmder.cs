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
    internal class RestoreFilesCmder : AbsHashesCmder
    {
        private RelayCommand selectFolderCmd;
        private RelayCommand restoreTaggedFilesCmd;
        private RelayCommand showHashesInFileTagCmd;
        private RelayCommand hideHashesInFileTagCmd;
        private string directoryUsedToSaveFiles;
        private bool saveToOriginDirectory = true;

        private static readonly SizeDelegates delegates = new SizeDelegates()
        {
            GetWindowWidth = () => Settings.Current.RestoreFilesProgressWidth,
            SetWindowWidth = width => Settings.Current.RestoreFilesProgressWidth = width,
            GetWindowHeight = () => Settings.Current.RestoreFilesProgressHeight,
            SetWindowHeight = height => Settings.Current.RestoreFilesProgressHeight = height,
        };

        public override ContentControl UserInterface { get; }

        public override string Display => "显示哈希标记或还原文件";

        public override string Description => "从使用本程序生成的带哈希标记的文件中还原出原文件。";

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

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public RestoreFilesCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public RestoreFilesCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this.UserInterface = new RestoreFilesCmderCtrl(this);
        }

        public override void Reset()
        {
            this.HideHashesInFileTagAction(null);
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

        private async Task<string> RestoreTaggedFiles(IEnumerable<HashViewModel> models,
            DoubleProgressWindow doubleProgressWindow, DoubleProgressModel doubleProgressModel)
        {
            try
            {
                doubleProgressModel.TotalCount = models.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in models)
                    {
                        doubleProgressModel.CurrentValue = 0.0;
                        doubleProgressModel.CurrentString = model.FileName;
                        try
                        {
                            using (FileStream fileStream = model.FileInfo.OpenRead())
                            using (FileDataHelper fileDataHelper = new FileDataHelper(fileStream))
                            {
                                if (!fileDataHelper.TryGetTaggedFileDataInfo(out FileDataInfo info))
                                {
                                    goto NextRound;
                                }
                                string oldExtension = Path.GetExtension(model.FileName);
                                string oldNameNoExt = Path.GetFileNameWithoutExtension(model.FileName);
                                string newNameNoExt = Path.GetFileNameWithoutExtension(oldNameNoExt);
                                string newExtension = !oldExtension.Equals(".hcm", StringComparison.OrdinalIgnoreCase) ?
                                    oldExtension : Path.GetExtension(oldNameNoExt);
                                string outputDirectory = this.SaveToOriginDirectory ?
                                    model.FileInfo.DirectoryName : this.DirectoryUsedToSaveFiles;
                                int duplicate = -1;
                                string newFilePath;
                                do
                                {
                                    string newFileName = ++duplicate == 0 ? $"{newNameNoExt}{newExtension}" :
                                        $"{newNameNoExt}_{duplicate}{newExtension}";
                                    newFilePath = Path.Combine(outputDirectory, newFileName);
                                } while (File.Exists(newFilePath));
                                using (FileStream newFileStream = File.OpenWrite(newFilePath))
                                {
                                    fileDataHelper.RestoreTaggedFile(newFileStream, info, doubleProgressModel);
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

        private async void RestoreTaggedFilesAction(object param)
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
                        MessageBox.Show(MainWindow.This, "请输入还原出来的文件的保存目录完整路径！", "提示",
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
                            MessageBox.Show(MainWindow.This, "用于保存还原出来的文件的目录不存在且创建失败！", "错误",
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
                    Task<string> restoreTaggedFilesTask = this.RestoreTaggedFiles(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await restoreTaggedFilesTask;
                    if (string.IsNullOrEmpty(exceptionMessage))
                    {
                        Settings.Current.ShowHashInTagColumn = true;
                    }
                    else
                    {
                        MessageBox.Show(MainWindow.This, $"出现异常导致过程中断：{exceptionMessage}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
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

        public ICommand RestoreTaggedFilesCmd
        {
            get
            {
                if (this.restoreTaggedFilesCmd == null)
                {
                    this.restoreTaggedFilesCmd = new RelayCommand(this.RestoreTaggedFilesAction);
                }
                return this.restoreTaggedFilesCmd;
            }
        }

        private async Task<string> GetFilesTag(IEnumerable<HashViewModel> models,
            DoubleProgressWindow doubleProgressWindow, DoubleProgressModel doubleProgressModel)
        {
            try
            {
                doubleProgressModel.TotalCount = models.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in models)
                    {
                        doubleProgressModel.CurrentValue = 0.0;
                        doubleProgressModel.CurrentString = model.FileName;
                        try
                        {
                            using (FileStream fileStream = model.FileInfo.OpenRead())
                            using (FileDataHelper hcFileHelper = new FileDataHelper(fileStream))
                            {
                                if (hcFileHelper.TryGetTaggedFileDataInfo(out FileDataInfo information))
                                {
                                    model.AlgoNameInTag = information.AlgoName;
                                    model.HashValueInTag = information.HashBytes;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
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

        private async void ShowHashesInFileTagAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    IEnumerable<HashViewModel> targets = hashViewModels.Where(i => i.IsExecutionTarget);
                    DoubleProgressModel progressModel = new DoubleProgressModel(delegates);
                    DoubleProgressWindow progressWindow = new DoubleProgressWindow(progressModel)
                    {
                        Owner = MainWindow.This
                    };
                    Task<string> getFilesTagTask = this.GetFilesTag(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await getFilesTagTask;
                    if (string.IsNullOrEmpty(exceptionMessage))
                    {
                        Settings.Current.ShowHashInTagColumn = true;
                    }
                    else
                    {
                        MessageBox.Show(MainWindow.This, $"出现异常导致过程中断：{exceptionMessage}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(MainWindow.This, "没有找到任何操作目标，请刷新筛选或手动勾选操作目标！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Settings.Current.FilterOrCmderEnabled = true;
                Settings.Current.ShowExecutionTargetColumn = false;
            }
        }

        public ICommand ShowHashesInFileTagCmd
        {
            get
            {
                if (this.showHashesInFileTagCmd == null)
                {
                    this.showHashesInFileTagCmd = new RelayCommand(this.ShowHashesInFileTagAction);
                }
                return this.showHashesInFileTagCmd;
            }
        }

        private void HideHashesInFileTagAction(object param)
        {
            Settings.Current.ShowHashInTagColumn = false;
            if (this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                foreach (HashViewModel model in hashViewModels)
                {
                    model.AlgoNameInTag = null;
                    model.HashValueInTag = null;
                }
            }
        }

        public ICommand HideHashesInFileTagCmd
        {
            get
            {
                if (this.hideHashesInFileTagCmd == null)
                {
                    this.hideHashesInFileTagCmd = new RelayCommand(this.HideHashesInFileTagAction);
                }
                return this.hideHashesInFileTagCmd;
            }
        }
    }
}
