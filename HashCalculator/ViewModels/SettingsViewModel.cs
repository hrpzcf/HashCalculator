using System;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace HashCalculator
{
    public class SettingsViewModel : NotifiableModel
    {
        private bool mainWndTopmost = false;
        private string lastUsedPath = string.Empty;
        private double mainWndWidth = 1280.0;
        private double mainWndHeight = 800.0;
        private double mainWndTop = double.NaN;
        private double mainWndLeft = double.NaN;
        private WindowState mainWindowState = WindowState.Normal;
        private double settingsWndWidth = 400.0;
        private double settingsWndHeight = 280.0;
        private AlgoType selectedAlgorithm = AlgoType.SHA1;
        private OutputType selectedOutputType = OutputType.BinaryUpper;
        private TaskNum selectedTaskNumberLimit = TaskNum.Two;
        private bool showResultText = false;
        private bool noExportColumn = false;
        private bool noDurationColumn = false;
        private bool noFileSizeColumn = false;
        private bool runInMultiInstanceMode = false;
        private bool notSettingShellExtension = true;
        private RelayCommand installContextMenuCmd;
        private RelayCommand unInstallContextMenuCmd;

        public SettingsViewModel()
        {
            if (double.IsNaN(this.mainWndLeft))
            {
                this.mainWndLeft = (SystemParameters.PrimaryScreenWidth
                    - this.mainWndWidth) / 2;
            }
            if (double.IsNaN(this.mainWndTop))
            {
                this.mainWndTop = (SystemParameters.PrimaryScreenHeight
                    - this.mainWndHeight) / 2;
            }
        }

        public bool MainWndTopmost
        {
            get
            {
                return this.mainWndTopmost;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndTopmost, value);
            }
        }

        public double MainWindowTop
        {
            get
            {
                return this.mainWndTop;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndTop, value);
            }
        }

        public double MainWindowLeft
        {
            get
            {
                return this.mainWndLeft;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndLeft, value);
            }
        }

        public double MainWndWidth
        {
            get
            {
                return this.mainWndWidth;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndWidth, value);
            }
        }

        public double MainWndHeight
        {
            get
            {
                return this.mainWndHeight;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndHeight, value);
            }
        }

        public WindowState MainWindowState
        {
            get
            {
                return this.mainWindowState;
            }
            set
            {
                this.SetPropNotify(ref this.mainWindowState, value);
            }
        }

        public double SettingsWndWidth
        {
            get
            {
                return this.settingsWndWidth;
            }
            set
            {
                this.SetPropNotify(ref this.settingsWndWidth, value);
            }
        }

        public double SettingsWndHeight
        {
            get
            {
                return this.settingsWndHeight;
            }
            set
            {
                this.SetPropNotify(ref this.settingsWndHeight, value);
            }
        }

        public AlgoType SelectedAlgo
        {
            get
            {
                return this.selectedAlgorithm;
            }
            set
            {
                this.SetPropNotify(ref this.selectedAlgorithm, value);
            }
        }

        public OutputType SelectedOutputType
        {
            get
            {
                return this.selectedOutputType;
            }
            set
            {
                this.SetPropNotify(ref this.selectedOutputType, value);
            }
        }

        public SearchPolicy SelectedQVSPolicy { get; set; }

        public SearchPolicy SelectedSearchPolicy { get; set; }

        public TaskNum SelectedTaskNumberLimit
        {
            get
            {
                return this.selectedTaskNumberLimit;
            }
            set
            {
                this.SetPropNotify(ref this.selectedTaskNumberLimit, value);
            }
        }

        public bool ShowResultText
        {
            get
            {
                return this.showResultText;
            }
            set
            {
                this.SetPropNotify(ref this.showResultText, value);
            }
        }

        public bool NoExportColumn
        {
            get
            {
                return this.noExportColumn;
            }
            set
            {
                this.SetPropNotify(ref this.noExportColumn, value);
            }
        }

        public bool NoDurationColumn
        {
            get
            {
                return this.noDurationColumn;
            }
            set
            {
                this.SetPropNotify(ref this.noDurationColumn, value);
            }
        }

        public bool NoFileSizeColumn
        {
            get
            {
                return this.noFileSizeColumn;
            }
            set
            {
                this.SetPropNotify(ref this.noFileSizeColumn, value);
            }
        }

        public bool PermanentlyDeleteFiles { get; set; }

        public bool RunInMultiInstMode
        {
            get
            {
                return this.runInMultiInstanceMode;
            }
            set
            {
                this.SetPropNotify(ref this.runInMultiInstanceMode, value);
            }
        }

        public string LastUsedPath
        {
            get
            {
                if (string.IsNullOrEmpty(this.lastUsedPath))
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                return this.lastUsedPath;
            }
            set
            {
                this.lastUsedPath = value;
            }
        }

        [XmlIgnore]
        public bool NotSettingShellExtension
        {
            get
            {
                return this.notSettingShellExtension;
            }
            set
            {
                this.SetPropNotify(ref this.notSettingShellExtension, value);
            }
        }

        private async void InstallContextMenuAction(object param)
        {
            if (MessageBox.Show(
                SettingsPanel.This,
                "安装右键菜单扩展可能需要重启资源管理器，确定现在安装吗？",
                "询问",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.InstallContextMenu() is Exception exception)
            {
                MessageBox.Show(
                    SettingsPanel.This, $"安装右键菜单扩展失败：\n{exception.Message}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    SettingsPanel.This, $"右键菜单扩展安装成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.NotSettingShellExtension = true;
        }

        [XmlIgnore]
        public ICommand InstallContextMenuCmd
        {
            get
            {
                if (this.installContextMenuCmd == null)
                {
                    this.installContextMenuCmd = new RelayCommand(this.InstallContextMenuAction);
                }
                return this.installContextMenuCmd;
            }
        }

        private async void UnInstallContextMenuAction(object param)
        {
            if (MessageBox.Show(
                SettingsPanel.This,
                "卸载右键菜单扩展可能需要重启资源管理器，确定现在卸载吗？",
                "询问",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.UninstallContextMenu() is Exception exception)
            {
                MessageBox.Show(
                    SettingsPanel.This, $"卸载右键菜单扩展失败：\n{exception.Message}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    SettingsPanel.This, $"右键菜单扩展卸载成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.NotSettingShellExtension = true;
        }

        [XmlIgnore]
        public ICommand UnInstallContextMenuCmd
        {
            get
            {
                if (this.unInstallContextMenuCmd == null)
                {
                    this.unInstallContextMenuCmd = new RelayCommand(this.UnInstallContextMenuAction);
                }
                return this.unInstallContextMenuCmd;
            }
        }

        [XmlIgnore]
        public ControlItem[] AvailableAlgos { get; } =
        {
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA1), AlgoType.SHA1),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA224), AlgoType.SHA224),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA256), AlgoType.SHA256),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA384), AlgoType.SHA384),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA512), AlgoType.SHA512),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA3_224), AlgoType.SHA3_224),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA3_256), AlgoType.SHA3_256),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA3_384), AlgoType.SHA3_384),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.SHA3_512), AlgoType.SHA3_512),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.MD5), AlgoType.MD5),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.BLAKE2S), AlgoType.BLAKE2S),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.BLAKE2B), AlgoType.BLAKE2B),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.BLAKE3), AlgoType.BLAKE3),
            new ControlItem(AlgoMap.GetAlgoName(AlgoType.WHIRLPOOL), AlgoType.WHIRLPOOL),
        };

        [XmlIgnore]
        public ControlItem[] AvailableOutputTypes { get; } =
        {
            new ControlItem("Base64 格式", OutputType.BASE64),
            new ControlItem("十六进制大写", OutputType.BinaryUpper),
            new ControlItem("十六进制小写", OutputType.BinaryLower),
        };

        [XmlIgnore]
        public ControlItem[] AvailableTaskNumLimits { get; } =
        {
            new ControlItem("1 个：大多数文件很大", TaskNum.One),
            new ControlItem("2 个：大多数文件较大", TaskNum.Two),
            new ControlItem("4 个：大多数文件较小", TaskNum.Four),
            new ControlItem("8 个：大多数文件很小", TaskNum.Eight),
        };

        [XmlIgnore]
        public ControlItem[] AvailableDroppedSearchPolicies { get; } =
        {
            new ControlItem("搜索一代子文件", SearchPolicy.Children),
            new ControlItem("搜索全部子文件", SearchPolicy.Descendants),
            new ControlItem("不搜索该文件夹", SearchPolicy.DontSearch),
        };

        [XmlIgnore]
        public ControlItem[] AvailableQVSearchPolicies { get; } =
        {
            new ControlItem("搜索依据所在目录的一代子文件", SearchPolicy.Children),
            new ControlItem("搜索依据所在目录的所有子文件", SearchPolicy.Descendants),
        };
    }
}
