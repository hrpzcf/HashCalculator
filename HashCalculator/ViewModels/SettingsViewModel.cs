using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace HashCalculator
{
#pragma warning disable IDE0180 // 使用元组交换值

    public class SettingsViewModel : NotifiableModel
    {
        private double mainWndWidth = 1100.0;
        private double mainWndHeight = 760.0;
        private double mainWndTop = double.NaN;
        private double mainWndLeft = double.NaN;
        private double settingsWndWidth = 680.0;
        private double settingsWndHeight = 560.0;
        private double algosPanelWidth = 450.0;
        private double algosPanelHeight = 410.0;
        private double hashDetailsWidth = 1200.0;
        private double hashDetailsHeight = 800.0;
        private double filterAndCmderWndWidth = 540.0;
        private double filterAndCmderWndHeight = 460.0;
        private double filterAndCmderWndTop = double.NaN;
        private double filterAndCmderWndLeft = double.NaN;
        private double shellMenuEditorWidth = 600.0;
        private double shellMenuEditorHeight = 400.0;
        private double shellSubmenuEditorWidth = 400.0;
        private double shellSubmenuEditorHeight = 600.0;
        private double mainWndDelFileProgressWidth = 400.0;
        private double mainWndDelFileProgressHeight = 200.0;
        private double markFilesProgressWidth = 400.0;
        private double markFilesProgressHeight = 200.0;
        private double restoreFilesProgressWidth = 400.0;
        private double restoreFilesProgressHeight = 200.0;

        private bool mainWndTopmost = false;
        private bool showFileIcon = true;
        private bool showResultText = false;
        private bool noSerialNumColumn = false;
        private bool noFullPathColumn = false;
        private bool noFileSizeColumn = false;
        private bool noOutputTypeColumn = false;
        private bool noDurationColumn = false;
        private bool noExportColumn = false;
        private bool noCmpResultColumn = false;
        private bool isMainRowSelectedByCheckBox = false;
        private bool showHashInTagColumn = false;
        private bool generateTextInFormat = false;
        private bool filterOrCmderEnabled = true;
        private bool runInMultiInstanceMode = false;
        private bool processingShellExtension = false;
        private bool preferChecklistAlgs = true;
        private bool parallelBetweenAlgos = true;
        private bool displayMainWndButtonText = true;
        private bool useDefaultOutputTypeWhenExporting = true;
        private bool monitorNewHashStringInClipboard = true;
        private bool switchMainWndFgWhenNewHashCopied = true;
        private bool filterAndCmderWndFollowsMainWnd = false;
        private bool caseOfCopiedAlgNameFollowsOutputType = false;
        private bool exportInMainControlsChildExportsInRow = false;
        private bool useUnixStyleLineBreaks = true;
        private bool eachAlgoExportedToSeparateFile = false;
        private bool askUserHowToExportResultsEveryTime = true;
        private bool delayTheStartOfCalculationTasks = false;
        private bool markTheSameHashValues = false;

        private CmpRes algoToSwitchToAfterHashChecked = CmpRes.Matched;
        private ExportAlgo howToExportHashValues = ExportAlgo.AllCalculated;
        private FetchAlgoOption fetchAlgorithmOption = FetchAlgoOption.TATSAMSHDL;
        private OutputType selectedOutputType = OutputType.BinaryUpper;
        private WindowState mainWindowState = WindowState.Normal;
        private ConfigLocation locationForSavingConfigFiles = ConfigLocation.Unset;

        private string lastUsedPath = string.Empty;
        private string displayingActiveConfigDir = null;
        private string displayingShellExtensionDir = null;
        private string displayingShellInstallationScope = null;
        private string displayingShellInstallationState = null;
        private string formatForGenerateText = "#$algo$ *$hash$ *$name$";

        private AlgoInOutModel selectedAlgoInOutModel = AlgosPanelModel.ProvidedAlgos[0];
        private TemplateForExportModel selectedExportTemplate;
        private TemplateForChecklistModel selectedChecklistTemplate;
        private ObservableCollection<TemplateForExportModel> templatesForExport = null;
        private ObservableCollection<TemplateForChecklistModel> templatesForChecklist = null;

        private int selectedTaskNumberLimit = 1;
        private int minCopiedCharsToTriggerHashCheck = 8;
        private int maxCopiedCharsToTriggerHashCheck = 512;
        private int millisecondsOfDelayedStartup = 360;

        private RelayCommand installShellExtCmd;
        private RelayCommand unInstallShellExtCmd;
        private RelayCommand openEditContextMenuCmd;

        private RelayCommand resetExportTemplateCmd;
        private RelayCommand addExportTemplateCmd;
        private RelayCommand copyExportTemplateCmd;
        private RelayCommand moveExportTemplateUpCmd;
        private RelayCommand moveExportTemplateDownCmd;
        private RelayCommand removeExportTemplateCmd;

        private RelayCommand resetChecklistTemplateCmd;
        private RelayCommand addChecklistTemplateCmd;
        private RelayCommand copyChecklistTemplateCmd;
        private RelayCommand moveChecklistTemplateUpCmd;
        private RelayCommand moveChecklistTemplateDownCmd;
        private RelayCommand removeChecklistTemplateCmd;

        private RelayCommand resetAlgorithmAliasCmd;

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
            this.PropertyChanged += Settings.MoveConfigFiles;
        }

        public string PreviousVer { get; set; }

        public bool DoNotHashForEmptyFile { get; set; } = true;

        public bool MainWndTopmost
        {
            get => this.mainWndTopmost;
            set => this.SetPropNotify(ref this.mainWndTopmost, value);
        }

        public double MainWindowTop
        {
            get => this.mainWndTop;
            set
            {
                if (this.MainWindowState == WindowState.Normal)
                {
                    this.SetPropNotify(ref this.mainWndTop, value);
                }
            }
        }

        public double MainWindowLeft
        {
            get => this.mainWndLeft;
            set
            {
                if (this.MainWindowState == WindowState.Normal)
                {
                    this.SetPropNotify(ref this.mainWndLeft, value);
                }
            }
        }

        public double MainWndWidth
        {
            get => this.mainWndWidth;
            set
            {
                if (this.MainWindowState == WindowState.Normal)
                {
                    this.SetPropNotify(ref this.mainWndWidth, value);
                }
            }
        }

        public double MainWndHeight
        {
            get => this.mainWndHeight;
            set
            {
                if (this.MainWindowState == WindowState.Normal)
                {
                    this.SetPropNotify(ref this.mainWndHeight, value);
                }
            }
        }

        public WindowState MainWindowState
        {
            get => this.mainWindowState;
            set => this.SetPropNotify(ref this.mainWindowState, value);
        }

        public double SettingsWndWidth
        {
            get => this.settingsWndWidth;
            set => this.SetPropNotify(ref this.settingsWndWidth, value);
        }

        public double SettingsWndHeight
        {
            get => this.settingsWndHeight;
            set => this.SetPropNotify(ref this.settingsWndHeight, value);
        }

        public double AlgosPanelWidth
        {
            get => this.algosPanelWidth;
            set => this.SetPropNotify(ref this.algosPanelWidth, value);
        }

        public double AlgosPanelHeight
        {
            get => this.algosPanelHeight;
            set => this.SetPropNotify(ref this.algosPanelHeight, value);
        }

        public double HashDetailsWndWidth
        {
            get => this.hashDetailsWidth;
            set => this.SetPropNotify(ref this.hashDetailsWidth, value);
        }

        public double HashDetailsWndHeight
        {
            get => this.hashDetailsHeight;
            set => this.SetPropNotify(ref this.hashDetailsHeight, value);
        }

        public double FilterAndCmderWndWidth
        {
            get => this.filterAndCmderWndWidth;
            set => this.SetPropNotify(ref this.filterAndCmderWndWidth, value);
        }

        public double FilterAndCmderWndHeight
        {
            get => this.filterAndCmderWndHeight;
            set => this.SetPropNotify(ref this.filterAndCmderWndHeight, value);
        }

        public double FilterAndCmderWndTop
        {
            get => this.filterAndCmderWndTop;
            set => this.SetPropNotify(ref this.filterAndCmderWndTop, value);
        }

        public double FilterAndCmderWndLeft
        {
            get => this.filterAndCmderWndLeft;
            set => this.SetPropNotify(ref this.filterAndCmderWndLeft, value);
        }

        public double FilterPanelTopRelToMain { get; set; }

        public double FilterPanelLeftRelToMain { get; set; }

        public double ShellMenuEditorWidth
        {
            get => this.shellMenuEditorWidth;
            set => this.SetPropNotify(ref this.shellMenuEditorWidth, value);
        }

        public double ShellMenuEditorHeight
        {
            get => this.shellMenuEditorHeight;
            set => this.SetPropNotify(ref this.shellMenuEditorHeight, value);
        }

        public double ShellSubmenuEditorWidth
        {
            get => this.shellSubmenuEditorWidth;
            set => this.SetPropNotify(ref this.shellSubmenuEditorWidth, value);
        }

        public double ShellSubmenuEditorHeight
        {
            get => this.shellSubmenuEditorHeight;
            set => this.SetPropNotify(ref this.shellSubmenuEditorHeight, value);
        }

        public double MainWndDelFileProgressWidth
        {
            get => this.mainWndDelFileProgressWidth;
            set => this.SetPropNotify(ref this.mainWndDelFileProgressWidth, value);
        }

        public double MainWndDelFileProgressHeight
        {
            get => this.mainWndDelFileProgressHeight;
            set => this.SetPropNotify(ref this.mainWndDelFileProgressHeight, value);
        }

        public double MarkFilesProgressWidth
        {
            get => this.markFilesProgressWidth;
            set => this.SetPropNotify(ref this.markFilesProgressWidth, value);
        }

        public double MarkFilesProgressHeight
        {
            get => this.markFilesProgressHeight;
            set => this.SetPropNotify(ref this.markFilesProgressHeight, value);
        }

        public double RestoreFilesProgressWidth
        {
            get => this.restoreFilesProgressWidth;
            set => this.SetPropNotify(ref this.restoreFilesProgressWidth, value);
        }

        public double RestoreFilesProgressHeight
        {
            get => this.restoreFilesProgressHeight;
            set => this.SetPropNotify(ref this.restoreFilesProgressHeight, value);
        }

        public Dictionary<string, int> ColumnsOrder { get; set; }

        public OutputType SelectedOutputType
        {
            get => this.selectedOutputType;
            set => this.SetPropNotify(ref this.selectedOutputType, value);
        }

        public SearchMethod SelectedSearchMethodForDragDrop { get; set; } =
            SearchMethod.Descendants;

        public SearchMethod SelectedSearchMethodForChecklist { get; set; } =
            SearchMethod.Descendants;

        public int SelectedTaskNumberLimit
        {
            get
            {
                return this.selectedTaskNumberLimit;
            }
            set
            {
                this.SetPropNotify(ref this.selectedTaskNumberLimit, value);
                if (value != 1)
                {
                    this.ParallelBetweenAlgos = false;
                }
            }
        }

        public bool ShowFileIcon
        {
            get => this.showFileIcon;
            set => this.SetPropNotify(ref this.showFileIcon, value);
        }

        public bool ShowResultText
        {
            get => this.showResultText;
            set => this.SetPropNotify(ref this.showResultText, value);
        }

        public bool NoSerialNumColumn
        {
            get => this.noSerialNumColumn;
            set => this.SetPropNotify(ref this.noSerialNumColumn, value);
        }

        public bool NoFileSizeColumn
        {
            get => this.noFileSizeColumn;
            set => this.SetPropNotify(ref this.noFileSizeColumn, value);
        }

        public bool NoOutputTypeColumn
        {
            get => this.noOutputTypeColumn;
            set => this.SetPropNotify(ref this.noOutputTypeColumn, value);
        }

        public bool NoDurationColumn
        {
            get => this.noDurationColumn;
            set => this.SetPropNotify(ref this.noDurationColumn, value);
        }

        public bool NoExportColumn
        {
            get => this.noExportColumn;
            set => this.SetPropNotify(ref this.noExportColumn, value);
        }

        public bool NoCmpResultColumn
        {
            get => this.noCmpResultColumn;
            set => this.SetPropNotify(ref this.noCmpResultColumn, value);
        }

        public bool NoFullPathColumn
        {
            get => this.noFullPathColumn;
            set => this.SetPropNotify(ref this.noFullPathColumn, value);
        }

        public bool MarkTheSameHashValues
        {
            get => this.markTheSameHashValues;
            set => this.SetPropNotify(ref this.markTheSameHashValues, value);
        }

        [JsonIgnore, XmlIgnore]
        public bool IsMainRowSelectedByCheckBox
        {
            get => this.isMainRowSelectedByCheckBox;
            set => this.SetPropNotify(ref this.isMainRowSelectedByCheckBox, value);
        }

        [JsonIgnore, XmlIgnore]
        public bool FilterAndCmderEnabled
        {
            get => this.filterOrCmderEnabled;
            set => this.SetPropNotify(ref this.filterOrCmderEnabled, value);
        }

        [JsonIgnore, XmlIgnore]
        public bool ShowHashInTagColumn
        {
            get => this.showHashInTagColumn;
            set => this.SetPropNotify(ref this.showHashInTagColumn, value);
        }

        public bool PermanentlyDeleteFiles { get; set; }

        public bool RunInMultiInstMode
        {
            get => this.runInMultiInstanceMode;
            set => this.SetPropNotify(ref this.runInMultiInstanceMode, value);
        }

        public ExportAlgo HowToExportHashValues
        {
            get => this.howToExportHashValues;
            set => this.SetPropNotify(ref this.howToExportHashValues, value);
        }

        public bool UseDefaultOutputTypeWhenExporting
        {
            get => this.useDefaultOutputTypeWhenExporting;
            set => this.SetPropNotify(ref this.useDefaultOutputTypeWhenExporting, value);
        }

        public string LastSavedName { get; set; }

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

        public bool PreferChecklistAlgs
        {
            get => this.preferChecklistAlgs;
            set => this.SetPropNotify(ref this.preferChecklistAlgs, value);
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
                if (value)
                {
                    this.SelectedTaskNumberLimit = 1;
                }
            }
        }

        public bool MonitorNewHashStringInClipboard
        {
            get => this.monitorNewHashStringInClipboard;
            set => this.SetPropNotify(ref this.monitorNewHashStringInClipboard, value);
        }

        public bool SwitchMainWndFgWhenNewHashCopied
        {
            get => this.switchMainWndFgWhenNewHashCopied;
            set => this.SetPropNotify(ref this.switchMainWndFgWhenNewHashCopied, value);
        }

        public FetchAlgoOption FetchAlgorithmOption
        {
            get => this.fetchAlgorithmOption;
            set => this.SetPropNotify(ref this.fetchAlgorithmOption, value);
        }

        public bool DisplayMainWndButtonText
        {
            get => this.displayMainWndButtonText;
            set => this.SetPropNotify(ref this.displayMainWndButtonText, value);
        }

        public int MinCopiedCharsToTriggerHashCheck
        {
            get
            {
                return this.minCopiedCharsToTriggerHashCheck;
            }
            set
            {
                if (value > this.MaxCopiedCharsToTriggerHashCheck)
                {
                    int temp = this.MaxCopiedCharsToTriggerHashCheck;
                    this.MaxCopiedCharsToTriggerHashCheck = value;
                    value = temp;
                }
                this.SetPropNotify(ref this.minCopiedCharsToTriggerHashCheck, value);
            }
        }

        public int MaxCopiedCharsToTriggerHashCheck
        {
            get
            {
                return this.maxCopiedCharsToTriggerHashCheck;
            }
            set
            {
                if (value < this.MinCopiedCharsToTriggerHashCheck)
                {
                    int temp = this.MinCopiedCharsToTriggerHashCheck;
                    this.MinCopiedCharsToTriggerHashCheck = value;
                    value = temp;
                }
                this.SetPropNotify(ref this.maxCopiedCharsToTriggerHashCheck, value);
            }
        }

        public CmpRes AlgoToSwitchToAfterHashChecked
        {
            get => this.algoToSwitchToAfterHashChecked;
            set => this.SetPropNotify(ref this.algoToSwitchToAfterHashChecked, value);
        }

        public bool GenerateTextInFormat
        {
            get => this.generateTextInFormat;
            set => this.SetPropNotify(ref this.generateTextInFormat, value);
        }

        public string FormatForGenerateText
        {
            get => this.formatForGenerateText;
            set => this.SetPropNotify(ref this.formatForGenerateText, value);
        }

        public bool UseUnixStyleLineBreaks
        {
            get => this.useUnixStyleLineBreaks;
            set => this.SetPropNotify(ref this.useUnixStyleLineBreaks, value);
        }

        public bool EachAlgoExportedToSeparateFile
        {
            get => this.eachAlgoExportedToSeparateFile;
            set => this.SetPropNotify(ref this.eachAlgoExportedToSeparateFile, value);
        }

        public bool AskUserHowToExportResultsEveryTime
        {
            get => this.askUserHowToExportResultsEveryTime;
            set => this.SetPropNotify(ref this.askUserHowToExportResultsEveryTime, value);
        }

        public bool FilterAndCmderWndFollowsMainWnd
        {
            get => this.filterAndCmderWndFollowsMainWnd;
            set
            {
                this.FilterPanelTopRelToMain = this.FilterAndCmderWndTop - this.MainWindowTop;
                this.FilterPanelLeftRelToMain = this.FilterAndCmderWndLeft - this.MainWindowLeft;
                this.SetPropNotify(ref this.filterAndCmderWndFollowsMainWnd, value);
            }
        }

        public ConfigLocation LocationForSavingConfigFiles
        {
            get => this.locationForSavingConfigFiles;
            set => this.SetPropNotify(ref this.locationForSavingConfigFiles, value);
        }

        public bool ExportInMainControlsChildExports
        {
            get => this.exportInMainControlsChildExportsInRow;
            set => this.SetPropNotify(ref this.exportInMainControlsChildExportsInRow, value);
        }

        public bool CaseOfCopiedAlgNameFollowsOutputType
        {
            get => this.caseOfCopiedAlgNameFollowsOutputType;
            set => this.SetPropNotify(ref this.caseOfCopiedAlgNameFollowsOutputType, value);
        }

        public int MillisecondsOfDelayedStartup
        {
            get => this.millisecondsOfDelayedStartup;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                this.SetPropNotify(ref this.millisecondsOfDelayedStartup, value);
            }
        }

        public bool DelayTheStartOfCalculationTasks
        {
            get => this.delayTheStartOfCalculationTasks;
            set => this.SetPropNotify(ref this.delayTheStartOfCalculationTasks, value);
        }

        public AlgoType[] SelectedAlgos { get; set; }

        public Dictionary<AlgoType, string> AlgorithmAliasList { get; set; }

        public ObservableCollection<TemplateForExportModel> TemplatesForExport
        {
            get => this.templatesForExport;
            set => this.SetPropNotify(ref this.templatesForExport, value);
        }

        public ObservableCollection<TemplateForChecklistModel> TemplatesForChecklist
        {
            get => this.templatesForChecklist;
            set => this.SetPropNotify(ref this.templatesForChecklist, value);
        }

        [JsonIgnore, XmlIgnore]
        public string DisplayingActiveConfigDir
        {
            get => this.displayingActiveConfigDir;
            set => this.SetPropNotify(ref this.displayingActiveConfigDir, value);
        }

        [JsonIgnore, XmlIgnore]
        public string DisplayingShellExtensionDir
        {
            get => this.displayingShellExtensionDir;
            set => this.SetPropNotify(ref this.displayingShellExtensionDir, value);
        }

        [JsonIgnore, XmlIgnore]
        public string DisplayingShellInstallationScope
        {
            get => this.displayingShellInstallationScope;
            set => this.SetPropNotify(ref this.displayingShellInstallationScope, value);
        }

        [JsonIgnore, XmlIgnore]
        public string DisplayingShellInstallationState
        {
            get => this.displayingShellInstallationState;
            set => this.SetPropNotify(ref this.displayingShellInstallationState, value);
        }

        [JsonIgnore, XmlIgnore]
        public bool ProcessingShellExtension
        {
            get => this.processingShellExtension;
            set => this.SetPropNotify(ref this.processingShellExtension, value);
        }

        private async void InstallShellExtAction(object param)
        {
            if (MessageBox.Show(
                SettingsPanel.Current, "安装外壳扩展可能需要重启资源管理器，确定现在安装吗？", "询问",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                == MessageBoxResult.No)
            {
                return;
            }
            this.ProcessingShellExtension = true;
            if (await ShellExtHelper.InstallShellExtension() is Exception exception1)
            {
                MessageBox.Show(SettingsPanel.Current, exception1.Message, "安装失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(SettingsPanel.Current, $"安装外壳扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            if (!File.Exists(Settings.MenuConfigFile))
            {
                string exception = new ShellMenuEditorModel(SettingsPanel.Current).SaveMenuListToJsonFile();
                if (!string.IsNullOrEmpty(exception))
                {
                    MessageBox.Show(SettingsPanel.Current,
                        $"外壳扩展模块配置文件创建失败，快捷菜单将无法显示，原因：{exception}", "警告",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            this.ProcessingShellExtension = false;
        }

        [JsonIgnore, XmlIgnore]
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
                SettingsPanel.Current, "卸载外壳扩展可能需要重启资源管理器，确定现在卸载吗？", "询问",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                == MessageBoxResult.No)
            {
                return;
            }
            this.ProcessingShellExtension = true;
            if (await ShellExtHelper.UninstallShellExtension() is Exception exception)
            {
                MessageBox.Show(SettingsPanel.Current, exception.Message, "卸载失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(SettingsPanel.Current, $"卸载外壳扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.ProcessingShellExtension = false;
        }

        [JsonIgnore, XmlIgnore]
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

        private void OpenEditContextMenuAction(object param)
        {
            SettingsPanel.Current.Close();
            ShellMenuEditor shellextEditor = new ShellMenuEditor()
            {
                Owner = MainWindow.Current
            };
            shellextEditor.ShowDialog();
        }

        [JsonIgnore, XmlIgnore]
        public ICommand OpenEditContextMenuCmd
        {
            get
            {
                if (this.openEditContextMenuCmd == null)
                {
                    this.openEditContextMenuCmd = new RelayCommand(this.OpenEditContextMenuAction);
                }
                return this.openEditContextMenuCmd;
            }
        }

        [JsonIgnore, XmlIgnore]
        public AlgoInOutModel SelectedInOutModelForAlias
        {
            get => this.selectedAlgoInOutModel;
            set => this.SetPropNotify(ref this.selectedAlgoInOutModel, value);
        }

        [JsonIgnore, XmlIgnore]
        public TemplateForExportModel SelectedTemplateForExport
        {
            get => this.selectedExportTemplate;
            set => this.SetPropNotify(ref this.selectedExportTemplate, value);
        }

        [JsonIgnore, XmlIgnore]
        public TemplateForChecklistModel SelectedTemplateForChecklist
        {
            get => this.selectedChecklistTemplate;
            set => this.SetPropNotify(ref this.selectedChecklistTemplate, value);
        }

        private void AddExportTemplateAction(object param)
        {
            TemplateForExportModel model = new TemplateForExportModel();
            if (this.TemplatesForExport == null)
            {
                this.TemplatesForExport = new ObservableCollection<TemplateForExportModel>();
            }
            this.TemplatesForExport.Add(model);
            this.SelectedTemplateForExport = model;
        }

        [JsonIgnore, XmlIgnore]
        public ICommand AddExportTemplateCmd
        {
            get
            {
                if (this.addExportTemplateCmd == null)
                {
                    this.addExportTemplateCmd = new RelayCommand(this.AddExportTemplateAction);
                }
                return this.addExportTemplateCmd;
            }
        }

        private void CopyExportTemplateAction(object param)
        {
            if (this.TemplatesForExport != null)
            {
                if (this.SelectedTemplateForExport != null)
                {
                    TemplateForExportModel model = this.SelectedTemplateForExport.Copy("_复制");
                    this.TemplatesForExport.Add(model);
                    this.SelectedTemplateForExport = model;
                }
                else
                {
                    MessageBox.Show(SettingsPanel.Current, "没有选择任何方案！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand CopyExportTemplateCmd
        {
            get
            {
                if (this.copyExportTemplateCmd == null)
                {
                    this.copyExportTemplateCmd = new RelayCommand(this.CopyExportTemplateAction);
                }
                return this.copyExportTemplateCmd;
            }
        }

        private void MoveExportTemplateUpAction(object param)
        {
            if (this.TemplatesForExport != null)
            {
                int index;
                if ((index = this.TemplatesForExport.IndexOf(this.SelectedTemplateForExport)) != -1 &&
                    index > 0)
                {
                    int prevTemplateIndex = index - 1;
                    TemplateForExportModel selectedTemplate = this.SelectedTemplateForExport;
                    this.TemplatesForExport[index] = this.TemplatesForExport[prevTemplateIndex];
                    this.TemplatesForExport[prevTemplateIndex] = selectedTemplate;
                    this.SelectedTemplateForExport = selectedTemplate;
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand MoveExportTemplateUpCmd
        {
            get
            {
                if (this.moveExportTemplateUpCmd == null)
                {
                    this.moveExportTemplateUpCmd = new RelayCommand(this.MoveExportTemplateUpAction);
                }
                return this.moveExportTemplateUpCmd;
            }
        }

        private void MoveExportTemplateDownAction(object param)
        {
            if (this.TemplatesForExport != null)
            {
                int index;
                if ((index = this.TemplatesForExport.IndexOf(this.SelectedTemplateForExport)) != -1 &&
                    index < this.TemplatesForExport.Count - 1)
                {
                    int nextTemplateIndex = index + 1;
                    TemplateForExportModel selectedTemplate = this.SelectedTemplateForExport;
                    this.TemplatesForExport[index] = this.TemplatesForExport[nextTemplateIndex];
                    this.TemplatesForExport[nextTemplateIndex] = selectedTemplate;
                    this.SelectedTemplateForExport = selectedTemplate;
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand MoveExportTemplateDownCmd
        {
            get
            {
                if (this.moveExportTemplateDownCmd == null)
                {
                    this.moveExportTemplateDownCmd = new RelayCommand(this.MoveExportTemplateDownAction);
                }
                return this.moveExportTemplateDownCmd;
            }
        }

        private void RemoveExportTemplateAction(object param)
        {
            if (this.TemplatesForExport != null)
            {
                int index;
                if ((index = this.TemplatesForExport.IndexOf(this.SelectedTemplateForExport)) != -1)
                {
                    this.TemplatesForExport.RemoveAt(index);
                    if (index < this.TemplatesForExport.Count)
                    {
                        this.SelectedTemplateForExport = this.TemplatesForExport[index];
                    }
                    else if (index > 0)
                    {
                        this.SelectedTemplateForExport = this.TemplatesForExport[index - 1];
                    }
                }
                else
                {
                    MessageBox.Show(SettingsPanel.Current, "没有选择任何方案！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand RemoveExportTemplateCmd
        {
            get
            {
                if (this.removeExportTemplateCmd == null)
                {
                    this.removeExportTemplateCmd = new RelayCommand(this.RemoveExportTemplateAction);
                }
                return this.removeExportTemplateCmd;
            }
        }

        internal void ResetTemplatesForExport()
        {
            this.SelectedTemplateForExport = null;
            this.TemplatesForExport = new ObservableCollection<TemplateForExportModel>
            {
                TemplateForExportModel.TxtModel.Copy(null),
                TemplateForExportModel.CsvModel.Copy(null),
                TemplateForExportModel.HcbModel.Copy(null),
                TemplateForExportModel.SfvModel.Copy(null),
                TemplateForExportModel.AllModel.Copy(null)
            };
        }

        private void ResetExportTemplateAction(object param)
        {
            this.ResetTemplatesForExport();
            MessageBox.Show(SettingsPanel.Current, "已重置导出结果方案列表。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [JsonIgnore, XmlIgnore]
        public ICommand ResetExportTemplateCmd
        {
            get
            {
                if (this.resetExportTemplateCmd == null)
                {
                    this.resetExportTemplateCmd = new RelayCommand(this.ResetExportTemplateAction);
                }
                return this.resetExportTemplateCmd;
            }
        }

        private void AddChecklistTemplateAction(object param)
        {
            TemplateForChecklistModel model = new TemplateForChecklistModel();
            if (this.TemplatesForChecklist == null)
            {
                this.TemplatesForChecklist = new ObservableCollection<TemplateForChecklistModel>();
            }
            this.TemplatesForChecklist.Add(model);
            this.SelectedTemplateForChecklist = model;
        }

        [JsonIgnore, XmlIgnore]
        public ICommand AddChecklistTemplateCmd
        {
            get
            {
                if (this.addChecklistTemplateCmd == null)
                {
                    this.addChecklistTemplateCmd = new RelayCommand(this.AddChecklistTemplateAction);
                }
                return this.addChecklistTemplateCmd;
            }
        }

        private void CopyChecklistTemplateAction(object param)
        {
            if (this.TemplatesForChecklist != null)
            {
                if (this.SelectedTemplateForChecklist != null)
                {
                    TemplateForChecklistModel model = this.SelectedTemplateForChecklist.Copy("_复制");
                    this.TemplatesForChecklist.Add(model);
                    this.SelectedTemplateForChecklist = model;
                }
                else
                {
                    MessageBox.Show(SettingsPanel.Current, "没有选择任何方案！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand CopyChecklistTemplateCmd
        {
            get
            {
                if (this.copyChecklistTemplateCmd == null)
                {
                    this.copyChecklistTemplateCmd = new RelayCommand(this.CopyChecklistTemplateAction);
                }
                return this.copyChecklistTemplateCmd;
            }
        }

        private void MoveChecklistTemplateUpAction(object param)
        {
            if (this.TemplatesForChecklist != null)
            {
                int index;
                if ((index = this.TemplatesForChecklist.IndexOf(this.SelectedTemplateForChecklist)) != -1 &&
                    index > 0)
                {
                    int prevTemplateIndex = index - 1;
                    TemplateForChecklistModel selectedTemplate = this.SelectedTemplateForChecklist;
                    this.TemplatesForChecklist[index] = this.TemplatesForChecklist[prevTemplateIndex];
                    this.TemplatesForChecklist[prevTemplateIndex] = selectedTemplate;
                    this.SelectedTemplateForChecklist = selectedTemplate;
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand MoveChecklistTemplateUpCmd
        {
            get
            {
                if (this.moveChecklistTemplateUpCmd == null)
                {
                    this.moveChecklistTemplateUpCmd = new RelayCommand(this.MoveChecklistTemplateUpAction);
                }
                return this.moveChecklistTemplateUpCmd;
            }
        }

        private void MoveChecklistTemplateDownAction(object param)
        {
            if (this.TemplatesForChecklist != null)
            {
                int index;
                if ((index = this.TemplatesForChecklist.IndexOf(this.SelectedTemplateForChecklist)) != -1 &&
                    index < this.TemplatesForChecklist.Count - 1)
                {
                    int nextTemplateIndex = index + 1;
                    TemplateForChecklistModel selectedTemplate = this.SelectedTemplateForChecklist;
                    this.TemplatesForChecklist[index] = this.TemplatesForChecklist[nextTemplateIndex];
                    this.TemplatesForChecklist[nextTemplateIndex] = selectedTemplate;
                    this.SelectedTemplateForChecklist = selectedTemplate;
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand MoveChecklistTemplateDownCmd
        {
            get
            {
                if (this.moveChecklistTemplateDownCmd == null)
                {
                    this.moveChecklistTemplateDownCmd = new RelayCommand(this.MoveChecklistTemplateDownAction);
                }
                return this.moveChecklistTemplateDownCmd;
            }
        }

        private void RemoveChecklistTemplateAction(object param)
        {
            if (this.TemplatesForChecklist != null)
            {
                int index;
                if ((index = this.TemplatesForChecklist.IndexOf(this.SelectedTemplateForChecklist)) != -1)
                {
                    this.TemplatesForChecklist.RemoveAt(index);
                    if (index < this.TemplatesForChecklist.Count)
                    {
                        this.SelectedTemplateForChecklist = this.TemplatesForChecklist[index];
                    }
                    else if (index > 0)
                    {
                        this.SelectedTemplateForChecklist = this.TemplatesForChecklist[index - 1];
                    }
                }
                else
                {
                    MessageBox.Show(SettingsPanel.Current, "没有选择任何方案！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public ICommand RemoveChecklistTemplateCmd
        {
            get
            {
                if (this.removeChecklistTemplateCmd == null)
                {
                    this.removeChecklistTemplateCmd = new RelayCommand(this.RemoveChecklistTemplateAction);
                }
                return this.removeChecklistTemplateCmd;
            }
        }

        internal void ResetTemplatesForChecklist()
        {
            this.SelectedTemplateForChecklist = null;
            this.TemplatesForChecklist = new ObservableCollection<TemplateForChecklistModel>
            {
                TemplateForChecklistModel.TxtFile.Copy(null),
                TemplateForChecklistModel.CsvFile.Copy(null),
                TemplateForChecklistModel.HcbFile.Copy(null),
                TemplateForChecklistModel.SfvFile.Copy(null),
                TemplateForChecklistModel.SumsFile.Copy(null),
                TemplateForChecklistModel.HashFile.Copy(null),
                TemplateForChecklistModel.AnyFile1.Copy(null),
                TemplateForChecklistModel.AnyFile2.Copy(null),
                TemplateForChecklistModel.AnyFile3.Copy(null),
                TemplateForChecklistModel.AnyFile4.Copy(null)
            };
        }

        private void ResetChecklistTemplateAction(object param)
        {
            this.ResetTemplatesForChecklist();
            MessageBox.Show(SettingsPanel.Current, "已重置解析检验依据方案列表。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [JsonIgnore, XmlIgnore]
        public ICommand ResetChecklistTemplateCmd
        {
            get
            {
                if (this.resetChecklistTemplateCmd == null)
                {
                    this.resetChecklistTemplateCmd = new RelayCommand(this.ResetChecklistTemplateAction);
                }
                return this.resetChecklistTemplateCmd;
            }
        }

        private void ResetAlgorithmAliasAction(object param)
        {
            foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
            {
                model.ResetAlias();
            }
            MessageBox.Show(SettingsPanel.Current, "已将所有算法的别名恢复到默认状态！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [JsonIgnore, XmlIgnore]
        public ICommand ResetAlgorithmAliasCmd
        {
            get
            {
                if (this.resetAlgorithmAliasCmd == null)
                {
                    this.resetAlgorithmAliasCmd = new RelayCommand(this.ResetAlgorithmAliasAction);
                }
                return this.resetAlgorithmAliasCmd;
            }
        }

        [OnSerializing]
        internal void OnSettingsViewModelSerializing(StreamingContext context)
        {
            if (this.TemplatesForExport != null && !this.TemplatesForExport.Any())
            {
                // 非 null 但空，统一设置为 null
                this.TemplatesForExport = null;
            }
            if (this.TemplatesForChecklist != null && !this.TemplatesForChecklist.Any())
            {
                // 非 null 但空，统一设置为 null
                this.TemplatesForChecklist = null;
            }
            this.AlgorithmAliasList = AlgosPanelModel.ProvidedAlgos.Where(
                i => !string.IsNullOrWhiteSpace(i.AlgorithmAlias)).ToDictionary(
                j => j.AlgoType, k => k.AlgorithmAlias);
            if (!this.AlgorithmAliasList.Any())
            {
                // 内容为空，统一设置为 null
                this.AlgorithmAliasList = null;
            }
            this.SelectedAlgos = AlgosPanelModel.ProvidedAlgos.Where(i => i.Selected).Select(
                i => i.AlgoType).ToArray();
        }

        [OnDeserialized]
        internal void OnSettingsViewModelDeserialized(StreamingContext context)
        {
            if (this.TemplatesForExport == null || !this.TemplatesForExport.Any())
            {
                this.ResetTemplatesForExport();
            }
            if (this.TemplatesForChecklist == null || !this.TemplatesForChecklist.Any())
            {
                this.ResetTemplatesForChecklist();
            }
            if (this.AlgorithmAliasList != null)
            {
                foreach (var keyValuePair in this.AlgorithmAliasList)
                {
                    foreach (AlgoInOutModel inOut in AlgosPanelModel.ProvidedAlgos)
                    {
                        if (inOut.AlgoType == keyValuePair.Key)
                        {
                            inOut.AlgorithmAlias = keyValuePair.Value;
                            break;
                        }
                    }
                }
            }
            foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
            {
                model.Selected = this.SelectedAlgos?.Contains(model.AlgoType) ?? false;
            }
        }

        [JsonIgnore, XmlIgnore]
        public bool ClipboardUpdatedByMe { get; set; }

        [JsonIgnore, XmlIgnore]
        public static string FixAlgoDlls { get; } = "更新动态链接库";

        [JsonIgnore, XmlIgnore]
        public static string ShellExtDir { get; } = "安装位置";

        [JsonIgnore, XmlIgnore]
        public static string UpdateExePath { get; } = "更新程序路径";

        [JsonIgnore, XmlIgnore]
        public static string AlgosDllDir { get; } = "动态链接库目录";

        [JsonIgnore, XmlIgnore]
        public static GenericItemModel[] AvailableOutputTypes { get; } =
        {
            new GenericItemModel("Base64", OutputType.BASE64),
            new GenericItemModel("Hex大写", OutputType.BinaryUpper),
            new GenericItemModel("Hex小写", OutputType.BinaryLower),
        };

        [JsonIgnore, XmlIgnore]
        public static GenericItemModel[] AvailableOutputTypesLong { get; } =
        {
            new GenericItemModel("Base64 格式", OutputType.BASE64),
            new GenericItemModel("十六进制大写", OutputType.BinaryUpper),
            new GenericItemModel("十六进制小写", OutputType.BinaryLower),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableTaskNumLimits { get; } =
        {
            new GenericItemModel("1", 1),
            new GenericItemModel("2", 2),
            new GenericItemModel("4", 4),
            new GenericItemModel("8", 8),
            new GenericItemModel("16", 16),
            new GenericItemModel("32", 32),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableDroppedSearchMethods { get; } =
        {
            new GenericItemModel("搜索该文件夹的一代子文件", SearchMethod.Children),
            new GenericItemModel("搜索该文件夹的全部子文件", SearchMethod.Descendants),
            new GenericItemModel("不对该文件夹进行搜索操作", SearchMethod.DontSearch),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableQVSearchMethods { get; } =
        {
            new GenericItemModel("搜索依据所在目录的一代子文件", SearchMethod.Children),
            new GenericItemModel("搜索依据所在目录的所有子文件", SearchMethod.Descendants),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableFetchAlgoOptions { get; } =
        {
            new GenericItemModel("使用【默认算法】中被勾选的算法", FetchAlgoOption.SELECTED),
            new GenericItemModel("使用被勾选且可产生相应长度哈希值的算法", FetchAlgoOption.TATSAMSHDL),
            new GenericItemModel("使用所有可产生相应哈希长度的算法", FetchAlgoOption.TATMSHDL),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableResultsToSwitchTo { get; } =
        {
            new GenericItemModel("保持现状不执行自动切换操作", CmpRes.NoResult),
            new GenericItemModel("校验结果是【无关联】的算法", CmpRes.Unrelated),
            new GenericItemModel("校验结果是【已匹配】的算法", CmpRes.Matched),
            new GenericItemModel("校验结果是【不匹配】的算法", CmpRes.Mismatch),
            new GenericItemModel("校验结果是【不确定】的算法", CmpRes.Uncertain),
        };
    }
}
