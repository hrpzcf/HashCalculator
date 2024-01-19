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

        public override ContentControl UserInterface { get; }

        public override string Display => "显示文件哈希标记或还原";

        public override string Description => "从使用本程序生成的带哈希标记的文件中还原出不带标记的原文件。";

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

        private void RestoreTaggedFilesAction(object param)
        {

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
                doubleProgressModel.FilesCount = models.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in models)
                    {
                        doubleProgressModel.ProgressValue = 0.0;
                        doubleProgressModel.CurFileName = model.FileName;
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

        private async void ShowHashesInFileTagAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    IEnumerable<HashViewModel> models = hashViewModels.Where(i => i.IsExecutionTarget);
                    DoubleProgressModel progressModel = new DoubleProgressModel(false);
                    DoubleProgressWindow progressWindow = new DoubleProgressWindow(progressModel)
                    {
                        Owner = MainWindow.This
                    };
                    Task<string> getFilesTagTask = this.GetFilesTag(models, progressWindow, progressModel);
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
