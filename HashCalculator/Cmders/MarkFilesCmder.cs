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
        private RelayCommand generateMarkedFilesCmd;
        private string directoryUsedToSaveFiles;
        private EditFileOption markFilesOption;

        private static readonly SizeDelegates delegates = new SizeDelegates()
        {
            GetWindowWidth = () => Settings.Current.MarkFilesProgressWidth,
            SetWindowWidth = width => Settings.Current.MarkFilesProgressWidth = width,
            GetWindowHeight = () => Settings.Current.MarkFilesProgressHeight,
            SetWindowHeight = height => Settings.Current.MarkFilesProgressHeight = height,
        };

        public override ContentControl UserInterface { get; }

        public override string Display => "添加标记改变文件哈希值";

        public override string Description => "给文件添加哈希标记以改变其哈希值，部分文件可正常使用，一般用于避过网络平台的相同文件检测。\n" +
            "对于改变哈希值后不能正常使用的文件，用【还原被改变哈希值的文件】对其进行还原即可得到原文件。";

        public EditFileOption MarkFilesOption
        {
            get => this.markFilesOption;
            set => this.SetPropNotify(ref this.markFilesOption, value);
        }

        public string DirectoryUsedToSaveFiles
        {
            get => this.directoryUsedToSaveFiles;
            set => this.SetPropNotify(ref this.directoryUsedToSaveFiles, value);
        }

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

        private async Task<string> GenerateMarkedFiles(IEnumerable<HashViewModel> models,
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
                            string outputDirectory = string.Empty;
                            switch (this.MarkFilesOption)
                            {
                                case EditFileOption.OriginalFile:
                                    using (FileStream stream = model.Information.Open(FileMode.Open,
                                        FileAccess.Write))
                                    {
                                        new HcmDataHelper(stream).GenerateMarkedFile(model.CurrentInOutModel);
                                    }
                                    goto RoundEndsAndNext;
                                default:
                                case EditFileOption.NewInSameLocation:
                                    outputDirectory = model.Information.DirectoryName;
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
                            using (FileStream fileStream = model.Information.OpenRead())
                            using (FileStream newFileStream = File.Create(newFilePath))
                            {
                                HcmDataHelper hcmDataHelper = new HcmDataHelper(fileStream);
                                result = hcmDataHelper.GenerateMarkedFile(newFileStream, model.CurrentInOutModel,
                                    doubleProgressModel);
                            }
                            if (!result && File.Exists(newFilePath))
                            {
                                File.Delete(newFilePath);
                            }
                        }
                        catch (Exception)
                        {
                        }
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

        private async void GenerateMarkedFilesAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn &&
                Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (this.MarkFilesOption == EditFileOption.NewInNewLocation)
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
                    Task<string> genMarkedFilesTask = this.GenerateMarkedFiles(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await genMarkedFilesTask;
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

        public ICommand GenerateMarkedFilesCmd
        {
            get
            {
                if (this.generateMarkedFilesCmd == null)
                {
                    this.generateMarkedFilesCmd = new RelayCommand(this.GenerateMarkedFilesAction);
                }
                return this.generateMarkedFilesCmd;
            }
        }
    }
}
