﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    internal class RenameFileCmder : AbsHashesCmder
    {
        private readonly AlgoInOutModel[] _algos;
        private RelayCommand renameFilesCmd;
        private RenameFileMethod methodForRenameFile;
        private bool automaticallyFocusAlgorithm = true;
        private string fileNameSeparator = defaultSeparator;

        private const string defaultSeparator = " - ";

        public override ContentControl UserInterface { get; }

        public override string Display => "重命名操作目标所指文件";

        public override string Description => "使用指定算法和指定格式的哈希值作为文件名重命名操作目标所指的文件。";

        public GenericItemModel[] OutputTypes { get; } = new GenericItemModel[]
        {
            new GenericItemModel("十六进制小写", OutputType.BinaryLower),
            new GenericItemModel("十六进制大写", OutputType.BinaryUpper),
        };

        public OutputType BeingUsedOutput { get; set; } = OutputType.BinaryLower;

        public bool AutomaticallyFocusAlgorithm
        {
            get => this.automaticallyFocusAlgorithm;
            set => this.SetPropNotify(ref this.automaticallyFocusAlgorithm, value);
        }

        public AlgoType SelectedAlgorithmType { get; set; }

        public AlgoInOutModel[] AlgoInOutModels => this._algos;

        public string FileNameSeparator
        {
            get => this.fileNameSeparator;
            set => this.SetPropNotify(ref this.fileNameSeparator, value);
        }

        public RenameFileMethod MethodForRenameFile
        {
            get => this.methodForRenameFile;
            set => this.SetPropNotify(ref this.methodForRenameFile, value);
        }

        public bool CheckIfUsingDistinctFilesFilter { get; set; } = true;

        public RenameFileCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public RenameFileCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this._algos = AlgosPanelModel.ProvidedAlgos;
            this.SelectedAlgorithmType = this._algos[0].AlgoType;
            this.UserInterface = new RenameFileCmderCtrl(this);
        }

        private void RenameFilesAction(object param)
        {
            if (Settings.Current.FilterAndCmderEnabled &&
                this.RefModels is IEnumerable<HashViewModel> models)
            {
                Settings.Current.FilterAndCmderEnabled = false;
                if (!models.Any(i => i.IsExecutionTarget && i.AlgoInOutModels != null))
                {
                    MessageBox.Show(MainWindow.Current, "没有任何可重命名的目标文件", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    goto FinishingTouches;
                }
                else if (MessageBox.Show(
                    MainWindow.Current, "用哈希值作为文件名重命名操作目标所指的文件吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    goto FinishingTouches;
                }
                if (this.CheckIfUsingDistinctFilesFilter && !models.Where(i => i.Matched).All(
                    i => i.FileIndex != null))
                {
                    if (MessageBox.Show(
                        MainWindow.Current, "没有应用【有效的文件】筛选器，要继续操作吗？", "提示",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        goto FinishingTouches;
                    }
                }
                AlgoType focusedAlgorithmType = default(AlgoType);
                if (this.AutomaticallyFocusAlgorithm)
                {
                    HashViewModel model = models.FirstOrDefault(
                        i => i.Matched &&
                        i.CurrentInOutModel != null &&
                        i.CurrentInOutModel.AlgoType != AlgoType.UNKNOWN);
                    if (model != null)
                    {
                        focusedAlgorithmType = model.CurrentInOutModel.AlgoType;
                    }
                }
                else if (this.SelectedAlgorithmType != AlgoType.UNKNOWN)
                {
                    focusedAlgorithmType = this.SelectedAlgorithmType;
                }
                foreach (HashViewModel model in models)
                {
                    if (model.Result == HashResult.Succeeded &&
                        model.IsExecutionTarget &&
                        model.AlgoInOutModels != null &&
                        File.Exists(model.Information.FullName))
                    {
                        string hashString = null;
                        foreach (AlgoInOutModel algo in model.AlgoInOutModels)
                        {
                            if (algo.AlgoType == focusedAlgorithmType)
                            {
                                hashString = BytesToStrByOutputTypeCvt.Convert(algo.HashResult,
                                    this.BeingUsedOutput);
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(hashString) || hashString.Equals(model.FileName))
                        {
                            continue;
                        }
                        if (this.fileNameSeparator == null)
                        {
                            this.fileNameSeparator = defaultSeparator;
                        }
                        string nameNoExt, newNameNoExt;
                        switch (this.MethodForRenameFile)
                        {
                            case RenameFileMethod.AddToEnd:
                                nameNoExt = Path.GetFileNameWithoutExtension(model.FileName);
                                newNameNoExt = $"{nameNoExt}{this.fileNameSeparator}{hashString}";
                                break;
                            case RenameFileMethod.AddToFront:
                                nameNoExt = Path.GetFileNameWithoutExtension(model.FileName);
                                newNameNoExt = $"{hashString}{this.fileNameSeparator}{nameNoExt}";
                                break;
                            default:
                            case RenameFileMethod.ReplaceAll:
                                newNameNoExt = hashString;
                                break;
                        }
                        string fileNewPath = Path.Combine(model.Information.DirectoryName,
                            $"{newNameNoExt}{model.Information.Extension}");
                        int number = 1;
                    TryRenameFile:
                        try
                        {
                            // 有可能重命名只改变原文件名大小写，即“已存在”的是即将重命名的文件本身，
                            // 这种情况重命名是可以成功的，不需要添加序号后缀
                            if (!File.Exists(fileNewPath) ||
                                model.Information.FullName.Equals(fileNewPath, StringComparison.OrdinalIgnoreCase))
                            {
                                model.Information.MoveTo(fileNewPath);
                                model.FileName = model.Information.Name;
                            }
                            else
                            {
                                fileNewPath = Path.Combine(model.Information.DirectoryName,
                                    $"{newNameNoExt} ({++number}){model.Information.Extension}");
                                goto TryRenameFile;
                            }
                        }
                        catch (Exception) { }
                    }
                    model.IsExecutionTarget = false;
                }
            FinishingTouches:
                Settings.Current.FilterAndCmderEnabled = true;
                Settings.Current.IsMainRowSelectedByCheckBox = false;
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
