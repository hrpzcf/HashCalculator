using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HashCalculator
{
    internal class RenameFileCmder : HashViewCmder
    {
        private RelayCommand prepareFilesCmd;
        private RelayCommand renameFilesCmd;
        private RelayCommand cancelRenameCmd;
        private readonly AlgoInOutModel[] _algos;

        public override string Display => "使用哈希值作为文件名重命名文件";

        public override string Description => "使用哈希值作为文件名重命名筛选出来并勾选了【操作目标】列的文件";

        public ControlItem[] OutputTypes { get; } = new ControlItem[]
        {
            new ControlItem("十六进制小写", OutputType.BinaryLower),
            new ControlItem("十六进制大写", OutputType.BinaryUpper),
        };

        public OutputType UsingOutput { get; set; } = OutputType.BinaryLower;

        public AlgoInOutModel[] AlgoInOutModels { get => this._algos; }

        public AlgoType SelectedAlgo { get; set; }

        public RenameFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public RenameFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this._algos = AlgosPanelModel.ProvidedAlgos;
            this.SelectedAlgo = this._algos[0].AlgoType;
        }

        public override void Reset()
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = false;
                }
            }
            Settings.Current.NoExecutionTargetColumn = true;
        }

        public ICommand CancelRenameCmd
        {
            get
            {
                if (this.cancelRenameCmd == null)
                {
                    this.cancelRenameCmd = new RelayCommand(o => { this.Reset(); });
                }
                return this.cancelRenameCmd;
            }
        }

        private void RenameFilesAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                if (!models.Any(i => i.IsExecutionTarget && i.AlgoInOutModels != null))
                {
                    MessageBox.Show(MainWindow.This, "没有任何可重命名的目标文件", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (MessageBox.Show(MainWindow.This, "使用哈希值为文件名重命名已勾选【操作目标】的行吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
                foreach (HashViewModel model in models)
                {
                    if (model.IsExecutionTarget && model.FileIndex != null && model.AlgoInOutModels != null)
                    {
                        string newFileName = null;
                        foreach (AlgoInOutModel algo in model.AlgoInOutModels)
                        {
                            if (algo.AlgoType == this.SelectedAlgo)
                            {
                                newFileName = BytesToStrByOutputTypeCvt.Convert(algo.HashResult, this.UsingOutput);
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(newFileName))
                        {
                            string fileNewPath = Path.Combine(
                                model.FileInfo.DirectoryName, $"{newFileName}{model.FileInfo.Extension}");
                            try
                            {
                                model.FileInfo.MoveTo(fileNewPath);
                                model.FileName = model.FileInfo.Name;
                            }
                            catch (IOException)
                            {
                                // 不在第一次用 MoveTo 重命名前判断新文件是否存在的原因是：
                                // 有可能重命名只改变原文件名大小写，即“已存在”的是即将重命名的文件本身，
                                // 这种情况重命名是可以成功的，不需要添加序号后缀
                                int number = 1;
                                while (File.Exists(fileNewPath))
                                {
                                    fileNewPath = Path.Combine(
                                        model.FileInfo.DirectoryName, $"{newFileName}-{number++}{model.FileInfo.Extension}");
                                }
                                try
                                {
                                    model.FileInfo.MoveTo(fileNewPath);
                                    model.FileName = model.FileInfo.Name;
                                }
                                catch (Exception) { }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                Settings.Current.NoExecutionTargetColumn = true;
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

        private void PrepareFilesAction(object param)
        {
            if (this.RefModels is IEnumerable<HashViewModel> models)
            {
                foreach (HashViewModel model in models)
                {
                    model.IsExecutionTarget = model.Matched && model.FileIndex != null;
                }
                Settings.Current.NoExecutionTargetColumn = false;
            }
        }

        public ICommand PrepareFilesCmd
        {
            get
            {
                if (this.prepareFilesCmd == null)
                {
                    this.prepareFilesCmd = new RelayCommand(this.PrepareFilesAction);
                }
                return this.prepareFilesCmd;
            }
        }
    }
}
