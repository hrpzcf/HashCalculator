using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using HashCalculator.Others;
using HashCalculator.ViewModels.Windows;
using HashCalculator.Views.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Wpfctrls = Wpf.Ui.Controls;

namespace HashCalculator
{
    internal class MarkFilesCmder : AbsHashesCmder
    {
        private RelayCommand selectFolderCmd;
        private RelayCommand generateMarkedFilesCmd;
        private EditFileOption markFilesOption;
        private string directoryUsedToSaveFiles;

        public override ContentControl UserInterface { get; }

        public override string Display => "添加标记改变文件的哈希值";

        public override string Description => "给文件添加哈希标记以改变其哈希值，部分文件可正常使用，一般用于在某些" +
            "情况下避过相同文件检测。对于改变哈希值后不能正常使用的文件，用【还原被改变哈希值的文件】对其进行还原即可得到原文件。";

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

        public MarkFilesCmder() : this(HashModelStore.HashViewModels)
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
                this.selectFolderCmd ??= new RelayCommand(this.SelectFolderAction);
                return this.selectFolderCmd;
            }
        }

        private async Task<string> GenerateMarkedFiles(IEnumerable<HashViewModel> models,
            ProgressWindow doubleProgressWindow, ProgressWindowModel doubleProgressModel)
        {
            try
            {
                IEnumerable<HashViewModel> validModels = models.Where(
                    i => i.Result == HashResult.NoResult || i.Result == HashResult.Succeeded);
                doubleProgressModel.TotalCount = validModels.Count();
                await Task.Run(() =>
                {
                    foreach (HashViewModel model in validModels)
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
            if (Settings.Current.IsFiltersAndCmdersIdle &&
                this.RefModels is IEnumerable<HashViewModel> hashViewModels)
            {
                Settings.Current.IsFiltersAndCmdersIdle = false;
                if (this.MarkFilesOption == EditFileOption.NewInNewLocation)
                {
                    if (string.IsNullOrEmpty(this.DirectoryUsedToSaveFiles) ||
                        !Path.IsPathRooted(this.DirectoryUsedToSaveFiles))
                    {
                        NotificationSender.ShowMessageBox(
                            MainWindow.Current, "提示", "请输入生成的新文件的保存目录的完整路径！");
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
                            NotificationSender.ShowMessageBox(
                                MainWindow.Current, "错误", "用于保存生成的新文件的目录不存在且创建失败！");
                            goto FinishingTouches;
                        }
                    }
                }
                if (this.CheckIfUsingDistinctFilesFilter && !hashViewModels.Where(
                    i => i.Matched).All(i => i.FileIndex != null))
                {
                    if (NotificationSender.ShowMessageBox(
                        MainWindow.Current,
                        "提示",
                        "没有应用【有效的文件】筛选器，要继续操作吗？",
                        closeButtonText: "否",
                        primaryButtonText: "是") != Wpfctrls.ContentDialogResult.Primary)
                    {
                        goto FinishingTouches;
                    }
                }
                if (hashViewModels.Any(i => i.IsExecutionTarget))
                {
                    IEnumerable<HashViewModel> targets = hashViewModels.Where(i => i.IsExecutionTarget);
                    ProgressWindowModel progressModel = new ProgressWindowModel()
                    {
                        WindowTitle = "正在写入...",
                    };
                    ProgressWindow progressWindow = new ProgressWindow(progressModel)
                    {
                        Owner = MainWindow.Current
                    };
                    Task<string> genMarkedFilesTask = this.GenerateMarkedFiles(targets, progressWindow, progressModel);
                    progressWindow.ShowDialog();
                    string exceptionMessage = await genMarkedFilesTask;
                    if (!string.IsNullOrEmpty(exceptionMessage))
                    {
                        NotificationSender.ShowMessageBox(
                            MainWindow.Current, "错误", $"出现异常导致过程中断：{exceptionMessage}");
                        goto FinishingTouches;
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

        public ICommand GenerateMarkedFilesCmd
        {
            get
            {
                this.generateMarkedFilesCmd ??= new RelayCommand(this.GenerateMarkedFilesAction);
                return this.generateMarkedFilesCmd;
            }
        }

        public static GenericItemModel[] AvailableMarkFilesOptions { get; } = new GenericItemModel[]
        {
            new GenericItemModel("直接把哈希标记写入到原文件上", EditFileOption.OriginalFile),
            new GenericItemModel("在原文件所在目录创建副本并写入标记", EditFileOption.NewInSameLocation),
            new GenericItemModel("在以下目录创建副本并写入标记", EditFileOption.NewInNewLocation),
        };
    }
}
