using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class RenameFileCmder : AbsHashesCmder
    {
        private readonly AlgoInOutModel[] _algos;
        private RelayCommand renameFilesCmd;

        public override string Display => "使用哈希值作为文件名重命名文件";

        public override string Description => "使用指定算法、格式的哈希值作为文件名，重命名操作对象所指的文件";

        public GenericItemModel[] OutputTypes { get; } = new GenericItemModel[]
        {
            new GenericItemModel("十六进制小写", OutputType.BinaryLower),
            new GenericItemModel("十六进制大写", OutputType.BinaryUpper),
        };

        public OutputType BeingUsedOutput { get; set; } = OutputType.BinaryLower;

        public AlgoInOutModel[] AlgoInOutModels { get => this._algos; }

        public AlgoType SelectedAlgo { get; set; }

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public RenameFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public RenameFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this._algos = AlgosPanelModel.ProvidedAlgos;
            this.SelectedAlgo = this._algos[0].AlgoType;
        }

        private void RenameFilesAction(object param)
        {
            if (Settings.Current.ShowExecutionTargetColumn && Settings.Current.FilterOrCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> models)
            {
                Settings.Current.FilterOrCmderEnabled = false;
                if (!models.Any(i => i.IsExecutionTarget && i.AlgoInOutModels != null))
                {
                    MessageBox.Show(MainWindow.This, "没有任何可重命名的目标文件", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    goto FinishingTouches;
                }
                else if (MessageBox.Show(MainWindow.This, "用哈希值作为文件名重命名操作对象所指的文件吗？", "确认",
                     MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    goto FinishingTouches;
                }
                if (this.CheckIfUsingDistinctFilesFilter &&
                    !models.Where(i => i.Matched).All(i => i.FileIndex != null))
                {
                    if (MessageBox.Show(MainWindow.This, "并非所有行都经过【有效文件】的筛选，继续吗？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        goto FinishingTouches;
                    }
                }
                foreach (HashViewModel model in models)
                {
                    if (model.IsExecutionTarget && model.AlgoInOutModels != null)
                    {
                        string newFileName = null;
                        foreach (AlgoInOutModel algo in model.AlgoInOutModels)
                        {
                            if (algo.AlgoType == this.SelectedAlgo)
                            {
                                newFileName = BytesToStrByOutputTypeCvt.Convert(algo.HashResult, this.BeingUsedOutput);
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(newFileName) || newFileName.Equals(model.FileName))
                        {
                            continue;
                        }
                        string fileNewPath = Path.Combine(
                               model.FileInfo.DirectoryName, $"{newFileName}{model.FileInfo.Extension}");
                        int number = 1;
                    TryRenameFile:
                        try
                        {
                            // 有可能重命名只改变原文件名大小写，即“已存在”的是即将重命名的文件本身，
                            // 这种情况重命名是可以成功的，不需要添加序号后缀
                            if (!File.Exists(fileNewPath) || model.FileInfo.FullName.Equals(
                                fileNewPath, StringComparison.OrdinalIgnoreCase))
                            {
                                model.FileInfo.MoveTo(fileNewPath);
                                model.FileName = model.FileInfo.Name;
                            }
                            else
                            {
                                fileNewPath = Path.Combine(
                                    model.FileInfo.DirectoryName, $"{newFileName} ({++number}){model.FileInfo.Extension}");
                                goto TryRenameFile;
                            }
                        }
                        catch (Exception) { }
                    }
                    model.IsExecutionTarget = false;
                }
            FinishingTouches:
                Settings.Current.FilterOrCmderEnabled = true;
                Settings.Current.ShowExecutionTargetColumn = false;
            }
        }

        public ICommand RenameFilesCmd
        {
            get
            {
                if (this.renameFilesCmd == null)
                {
                    this.renameFilesCmd = new RelayCommand(this.RenameFilesAction);
                }
                return this.renameFilesCmd;
            }
        }
    }
}
