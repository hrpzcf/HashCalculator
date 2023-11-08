using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Policy;
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
        private double settingsWndWidth = 600.0;
        private double settingsWndHeight = 500.0;
        private double algosPanelWidth = 450.0;
        private double algosPanelHeight = 380.0;
        private double hashDetailsWidth = 1200.0;
        private double hashDetailsHeight = 800.0;
        private double cmdPanelWidth = 590.0;
        private double cmdPanelHeight = 565.0;
        private double cmdPanelTopRelToMainWnd = 0.0;
        private double cmdPanelLeftRelToMainWnd = 0.0;
        private TaskNum selectedTaskNumberLimit = TaskNum.One;
        private ExportType resultFileTypeExportAs = ExportType.TxtFile;
        private ExportAlgos howToExportHashValues = ExportAlgos.Current;
        private OutputType selectedOutputType = OutputType.BinaryUpper;
        private bool showResultText = false;
        private bool noExportColumn = false;
        private bool noDurationColumn = false;
        private bool noFileSizeColumn = false;
        private bool filterOrCmderEnabled = true;
        private bool showExecutionTargetColumn = false;
        private bool runInMultiInstanceMode = false;
        private bool notSettingShellExtension = true;
        private bool preferAlgosInBasis = true;
        private bool parallelBetweenAlgos = false;
        private bool monitorNewHashStringInClipboard = true;
        private RelayCommand installShellExtCmd;
        private RelayCommand unInstallShellExtCmd;

        [XmlIgnore]
        public bool ClipboardUpdatedByMe { get; set; }

        [XmlIgnore]
        public static string FixAlgoDlls { get; } = "更新动态链接库";

        [XmlIgnore]
        public static string StringDllDir { get; } = "动态链接库目录";

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
            this.AddSelectedAlgosChanged();
        }

        private void SelectedAlgosChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems.AnyItem() && e.NewItems[0] is AlgoType algo1)
                    {
                        foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
                        {
                            if (model.AlgoType == algo1)
                            {
                                model.Selected = true;
                                break;
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.AnyItem() && e.OldItems[0] is AlgoType algo2)
                    {
                        foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
                        {
                            if (model.AlgoType == algo2)
                            {
                                model.Selected = false;
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        internal void AddSelectedAlgosChanged()
        {
            this.SelectedAlgos.CollectionChanged += this.SelectedAlgosChanged;
        }

        internal void RemoveSelectedAlgosChanged()
        {
            this.SelectedAlgos.CollectionChanged -= this.SelectedAlgosChanged;
        }

        public string PreviousVer { get; set; }

        public bool DoNotHashForEmptyFile { get; set; } = true;

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

        public double AlgosPanelWidth
        {
            get
            {
                return this.algosPanelWidth;
            }
            set
            {
                this.SetPropNotify(ref this.algosPanelWidth, value);
            }
        }

        public double AlgosPanelHeight
        {
            get
            {
                return this.algosPanelHeight;
            }
            set
            {
                this.SetPropNotify(ref this.algosPanelHeight, value);
            }
        }

        public double HashDetailsWndWidth
        {
            get
            {
                return this.hashDetailsWidth;
            }
            set
            {
                this.SetPropNotify(ref this.hashDetailsWidth, value);
            }
        }

        public double HashDetailsWndHeight
        {
            get
            {
                return this.hashDetailsHeight;
            }
            set
            {
                this.SetPropNotify(ref this.hashDetailsHeight, value);
            }
        }

        public double CmdPanelWidth
        {
            get
            {
                return this.cmdPanelWidth;
            }
            set
            {
                this.SetPropNotify(ref this.cmdPanelWidth, value);
            }
        }

        public double CmdPanelHeight
        {
            get
            {
                return this.cmdPanelHeight;
            }
            set
            {
                this.SetPropNotify(ref this.cmdPanelHeight, value);
            }
        }

        public double CmdPanelTopRelToMainWnd
        {
            get
            {
                return this.cmdPanelTopRelToMainWnd;
            }
            set
            {
                this.SetPropNotify(ref this.cmdPanelTopRelToMainWnd, value);
            }
        }

        public double CmdPanelLeftRelToMainWnd
        {
            get
            {
                return this.cmdPanelLeftRelToMainWnd;
            }
            set
            {
                this.SetPropNotify(ref this.cmdPanelLeftRelToMainWnd, value);
            }
        }

        public ObservableCollection<AlgoType> SelectedAlgos { get; } =
            new ObservableCollection<AlgoType>();

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

        [XmlIgnore]
        public bool ShowExecutionTargetColumn
        {
            get
            {
                return this.showExecutionTargetColumn;
            }
            set
            {
                this.SetPropNotify(ref this.showExecutionTargetColumn, value);
            }
        }

        [XmlIgnore]
        public bool FilterOrCmderEnabled
        {
            get
            {
                return this.filterOrCmderEnabled;
            }
            set
            {
                this.SetPropNotify(ref this.filterOrCmderEnabled, value);
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

        public ExportAlgos HowToExportHashValues
        {
            get
            {
                return this.howToExportHashValues;
            }
            set
            {
                this.SetPropNotify(ref this.howToExportHashValues, value);
            }
        }

        public ExportType ResultFileTypeExportAs
        {
            get
            {
                return this.resultFileTypeExportAs;
            }
            set
            {
                this.SetPropNotify(ref this.resultFileTypeExportAs, value);
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

        public bool PreferAlgosInBasis
        {
            get
            {
                return this.preferAlgosInBasis;
            }
            set
            {
                this.SetPropNotify(ref this.preferAlgosInBasis, value);
            }
        }

        public bool ParallelBetweenAlgos
        {
            get
            {
                return this.parallelBetweenAlgos;
            }
            set
            {
                this.SetPropNotify(ref this.parallelBetweenAlgos, value);
            }
        }

        public bool MonitorNewHashStringInClipboard
        {
            get
            {
                return this.monitorNewHashStringInClipboard;
            }
            set
            {
                this.SetPropNotify(ref this.monitorNewHashStringInClipboard, value);
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

        private async void InstallShellExtAction(object param)
        {
            if (MessageBox.Show(
                SettingsPanel.This,
                "安装 Shell 扩展可能需要重启资源管理器，确定现在安装吗？",
                "询问",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.InstallShellExtension() is Exception exception)
            {
                MessageBox.Show(
                    SettingsPanel.This, $"安装 Shell 扩展失败：\n{exception.Message}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    SettingsPanel.This, $"安装 Shell 扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.NotSettingShellExtension = true;
        }

        [XmlIgnore]
        public ICommand InstallShellExtCmd
        {
            get
            {
                if (this.installShellExtCmd == null)
                {
                    this.installShellExtCmd = new RelayCommand(this.InstallShellExtAction);
                }
                return this.installShellExtCmd;
            }
        }

        private async void UnInstallShellExtAction(object param)
        {
            if (MessageBox.Show(
                SettingsPanel.This,
                "卸载 Shell 扩展可能需要重启资源管理器，确定现在卸载吗？",
                "询问",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.UninstallShellExtension() is Exception exception)
            {
                MessageBox.Show(
                    SettingsPanel.This, $"卸载 Shell 扩展失败：\n{exception.Message}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    SettingsPanel.This, $"卸载 Shell 扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.NotSettingShellExtension = true;
        }

        [XmlIgnore]
        public ICommand UnInstallShellExtCmd
        {
            get
            {
                if (this.unInstallShellExtCmd == null)
                {
                    this.unInstallShellExtCmd = new RelayCommand(this.UnInstallShellExtAction);
                }
                return this.unInstallShellExtCmd;
            }
        }

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
