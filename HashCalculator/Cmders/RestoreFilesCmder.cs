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
        private RelayCommand restoreMarkedFilesCmd;
        private RelayCommand showFilesHcmDataCmd;
        private RelayCommand hideFilesHcmDataCmd;
        private string directoryUsedToSaveFiles;
        private EditFileOption restoreFilesOption;

        private static readonly SizeDelegates delegates = new SizeDelegates()
        {
            GetWindowWidth = () => Settings.Current.RestoreFilesProgressWidth,
            SetWindowWidth = width => Settings.Current.RestoreFilesProgressWidth = width,
            GetWindowHeight = () => Settings.Current.RestoreFilesProgressHeight,
            SetWindowHeight = height => Settings.Current.RestoreFilesProgressHeight = height,
        };

        public override ContentControl UserInterface { get; }

        public override string Display => "还原被改变哈希值的文件";

        public override string Description => "功能一：在主窗口【哈希标记】列中显示被改变哈希值的文件内记录的原文件哈希值。\n" +
            "功能二：将被改变过哈希值的文件还原，没有用本程序改变过哈希值的文件将被忽略。";

        public EditFileOption RestoreFilesOption
        {
            get => this.restoreFilesOption;
            set => this.SetPropNotify(ref this.restoreFilesOption, value);
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
            this.HideFilesHcmDataAction(null);
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

        private async Task<string> RestoreMarkedFiles(IEnumerable<HashViewModel> models,
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
                            string outputDirectory = string.Empty;
                            switch (this.RestoreFilesOption)
                            {
                                case EditFileOption.OriginalFile:
                                    using (FileStream fileStream = model.FileInfo.Open(
                                        FileMode.Open, FileAccess.ReadWrite))
                                    {
                                        if (new HcmDataHelper(fileStream).RestoreMarkedFile())
                                        {
                                            model.HcmDataFromFile = null;
                                        }
                                    }
                                    goto RoundEndsAndNext;
                                default:
                                case EditFileOption.NewInSameLocation:
                                    outputDirectory = model.FileInfo.DirectoryName;
                                    break;
                                case EditFileOption.NewInNewLocation:
                                    outputDirectory = this.DirectoryUsedToSaveFiles;
                                    break;
                            }
                            string extension = Path.GetExtension(model.FileName);
                            string nameNoExt = Path.GetFileNameWithoutExtension(model.FileName);
                            int duplicate = -1;
                            string newFilePath;
                            do
                            {
                                string newFileName = ++duplicate == 0 ? $"{nameNoExt}{extension}" :
                                    $"{nameNoExt}_{duplicate}{extension}";
                                newFilePath = Path.Combine(outputDirectory, newFileName);
                            } while (File.Exists(newFilePath));
                            bool result = true;
                            using (FileStream fileStream = model.FileInfo.OpenRead())
                            using (FileStream newFileStream = File.Create(newFilePath))
                            {
                                HcmDataHelper hcmDataHelper = new HcmDataHelper(fileStream);
                                result = hcmDataHelper.RestoreMarkedFile(newFileStream, doubleProgressModel);
                            }
                            if (!result && File.Exists(newFilePath))
                            {
                                File.Delete(newFilePath);
                            }
                        }
                        catch (Exception) { }
                    RoundEndsAndNext:
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

        private async void RestoreMarkedFilesAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (this.RestoreFilesOption == EditFileOption.NewInNewLocation)
                {
                    if (string.IsNullOrEmpty(this.DirectoryUsedToSaveFiles) ||
                        !Path.IsPathRooted(this.DirectoryUsedToSaveFiles))
                    {
                        MessageBox.Show(MainWindow.This, "请输入还原的文件的保存目录完整路径！", "提示",
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
                            MessageBox.Show(MainWindow.This, "用于保存还原的文件的目录不存在且创建失败！", "错误",
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
                    Task<string> restoreMarkedFilesTask = this.RestoreMarkedFiles(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await restoreMarkedFilesTask;
                    if (!string.IsNullOrEmpty(exceptionMessage))
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

        public ICommand RestoreMarkedFilesCmd
        {
            get
            {
                if (this.restoreMarkedFilesCmd == null)
                {
                    this.restoreMarkedFilesCmd = new RelayCommand(this.RestoreMarkedFilesAction);
                }
                return this.restoreMarkedFilesCmd;
            }
        }

        private async Task<string> GetFilesHcmData(IEnumerable<HashViewModel> models,
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
                            {
                                if (!new HcmDataHelper(fileStream).ReadHcmData(out HcmData hcmData))
                                {
                                    model.HcmDataFromFile = null;
                                }
                                else
                                {
                                    model.HcmDataFromFile = hcmData;
                                }
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

        private async void ShowFilesHcmDataAction(object param)
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
                    Task<string> getFilesHcmDataTask = this.GetFilesHcmData(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await getFilesHcmDataTask;
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

        public ICommand ShowFilesHcmDataCmd
        {
            get
            {
                if (this.showFilesHcmDataCmd == null)
                {
                    this.showFilesHcmDataCmd = new RelayCommand(this.ShowFilesHcmDataAction);
                }
                return this.showFilesHcmDataCmd;
            }
        }

        private void HideFilesHcmDataAction(object param)
        {
            Settings.Current.ShowHashInTagColumn = false;
            if (this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                foreach (HashViewModel model in hashViewModels)
                {
                    model.HcmDataFromFile = null;
                }
            }
        }

        public ICommand HideFilesHcmDataCmd
        {
            get
            {
                if (this.hideFilesHcmDataCmd == null)
                {
                    this.hideFilesHcmDataCmd = new RelayCommand(this.HideFilesHcmDataAction);
                }
                return this.hideFilesHcmDataCmd;
            }
        }
    }
}
