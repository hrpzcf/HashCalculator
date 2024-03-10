using System;
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
        private bool mainWndTopmost = false;
        private string lastUsedPath = string.Empty;
        private double mainWndWidth = 1100.0;
        private double mainWndHeight = 700.0;
        private double mainWndTop = double.NaN;
        private double mainWndLeft = double.NaN;
        private WindowState mainWindowState = WindowState.Normal;
        private double settingsWndWidth = 660.0;
        private double settingsWndHeight = 565.0;
        private double algosPanelWidth = 450.0;
        private double algosPanelHeight = 410.0;
        private double hashDetailsWidth = 1200.0;
        private double hashDetailsHeight = 800.0;
        private double filterAndCmderWndWidth = 540.0;
        private double filterAndCmderWndHeight = 640.0;
        private double cmdPanelTopRelToMainWnd = 0.0;
        private double cmdPanelLeftRelToMainWnd = 0.0;
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
        private int selectedTaskNumberLimit = 1;
        private bool useDefaultOutputTypeWhenExporting = true;
        private OutputType selectedOutputType = OutputType.BinaryUpper;
        private ExportAlgo howToExportHashValues = ExportAlgo.AllCalculated;
        private bool showResultText = false;
        private bool noExportColumn = false;
        private bool noDurationColumn = false;
        private bool noFileSizeColumn = false;
        private bool noOutputTypeColumn = false;
        private bool showExecutionTargetColumn = false;
        private bool showHashInTagColumn = false;
        private bool filterOrCmderEnabled = true;
        private bool runInMultiInstanceMode = false;
        private bool notSettingShellExtension = true;
        private bool preferChecklistAlgs = true;
        private bool parallelBetweenAlgos = true;
        private bool monitorNewHashStringInClipboard = true;
        private bool switchMainWndFgWhenNewHashCopied = true;
        private CmpRes algoToSwitchToAfterHashChecked = CmpRes.Matched;
        private FetchAlgoOption fetchAlgorithmOption = FetchAlgoOption.TATSAMSHDL;
        private bool displayMainWndButtonText = true;
        private bool caseOfCopiedAlgNameFollowsOutputType = false;
        private bool generateTextInFormat = false;
        private string formatForGenerateText = "#$algo$ *$hash$ *$name$";
        private bool exportInMainControlsChildExportsInRow = false;
        private TemplateForExportModel selectedExportTemplate;
        private TemplateForChecklistModel selectedChecklistTemplate;
        private ObservableCollection<TemplateForExportModel> templatesForExport = null;
        private ObservableCollection<TemplateForChecklistModel> templatesForChecklist = null;
        private int minCopiedCharsToTriggerHashCheck = 8;
        private int maxCopiedCharsToTriggerHashCheck = 512;

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

        public double FilterAndCmderWndWidth
        {
            get
            {
                return this.filterAndCmderWndWidth;
            }
            set
            {
                this.SetPropNotify(ref this.filterAndCmderWndWidth, value);
            }
        }

        public double FilterAndCmderWndHeight
        {
            get
            {
                return this.filterAndCmderWndHeight;
            }
            set
            {
                this.SetPropNotify(ref this.filterAndCmderWndHeight, value);
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

        public double ShellMenuEditorWidth
        {
            get
            {
                return this.shellMenuEditorWidth;
            }
            set
            {
                this.SetPropNotify(ref this.shellMenuEditorWidth, value);
            }
        }

        public double ShellMenuEditorHeight
        {
            get
            {
                return this.shellMenuEditorHeight;
            }
            set
            {
                this.SetPropNotify(ref this.shellMenuEditorHeight, value);
            }
        }

        public double ShellSubmenuEditorWidth
        {
            get
            {
                return this.shellSubmenuEditorWidth;
            }
            set
            {
                this.SetPropNotify(ref this.shellSubmenuEditorWidth, value);
            }
        }

        public double ShellSubmenuEditorHeight
        {
            get
            {
                return this.shellSubmenuEditorHeight;
            }
            set
            {
                this.SetPropNotify(ref this.shellSubmenuEditorHeight, value);
            }
        }

        public double MainWndDelFileProgressWidth
        {
            get
            {
                return this.mainWndDelFileProgressWidth;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndDelFileProgressWidth, value);
            }
        }

        public double MainWndDelFileProgressHeight
        {
            get
            {
                return this.mainWndDelFileProgressHeight;
            }
            set
            {
                this.SetPropNotify(ref this.mainWndDelFileProgressHeight, value);
            }
        }

        public double MarkFilesProgressWidth
        {
            get
            {
                return this.markFilesProgressWidth;
            }
            set
            {
                this.SetPropNotify(ref this.markFilesProgressWidth, value);
            }
        }

        public double MarkFilesProgressHeight
        {
            get
            {
                return this.markFilesProgressHeight;
            }
            set
            {
                this.SetPropNotify(ref this.markFilesProgressHeight, value);
            }
        }

        public double RestoreFilesProgressWidth
        {
            get
            {
                return this.restoreFilesProgressWidth;
            }
            set
            {
                this.SetPropNotify(ref this.restoreFilesProgressWidth, value);
            }
        }

        public double RestoreFilesProgressHeight
        {
            get
            {
                return this.restoreFilesProgressHeight;
            }
            set
            {
                this.SetPropNotify(ref this.restoreFilesProgressHeight, value);
            }
        }

        public AlgoType[] SelectedAlgos { get; set; }

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

        public bool NoOutputTypeColumn
        {
            get
            {
                return this.noOutputTypeColumn;
            }
            set
            {
                this.SetPropNotify(ref this.noOutputTypeColumn, value);
            }
        }

        [JsonIgnore, XmlIgnore]
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

        [JsonIgnore, XmlIgnore]
        public bool ShowHashInTagColumn
        {
            get
            {
                return this.showHashInTagColumn;
            }
            set
            {
                this.SetPropNotify(ref this.showHashInTagColumn, value);
            }
        }

        [JsonIgnore, XmlIgnore]
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

        public ExportAlgo HowToExportHashValues
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

        public bool UseDefaultOutputTypeWhenExporting
        {
            get
            {
                return this.useDefaultOutputTypeWhenExporting;
            }
            set
            {
                this.SetPropNotify(ref this.useDefaultOutputTypeWhenExporting, value);
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

        public bool PreferChecklistAlgs
        {
            get
            {
                return this.preferChecklistAlgs;
            }
            set
            {
                this.SetPropNotify(ref this.preferChecklistAlgs, value);
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
                if (value)
                {
                    this.SelectedTaskNumberLimit = 1;
                }
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

        public bool SwitchMainWndFgWhenNewHashCopied
        {
            get
            {
                return this.switchMainWndFgWhenNewHashCopied;
            }
            set
            {
                this.SetPropNotify(ref this.switchMainWndFgWhenNewHashCopied, value);
            }
        }

        public FetchAlgoOption FetchAlgorithmOption
        {
            get
            {
                return this.fetchAlgorithmOption;
            }
            set
            {
                this.SetPropNotify(ref this.fetchAlgorithmOption, value);
            }
        }

        public bool DisplayMainWndButtonText
        {
            get
            {
                return this.displayMainWndButtonText;
            }
            set
            {
                this.SetPropNotify(ref this.displayMainWndButtonText, value);
            }
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
            get
            {
                return this.algoToSwitchToAfterHashChecked;
            }
            set
            {
                this.SetPropNotify(ref this.algoToSwitchToAfterHashChecked, value);
            }
        }

        public bool GenerateTextInFormat
        {
            get
            {
                return this.generateTextInFormat;
            }
            set
            {
                this.SetPropNotify(ref this.generateTextInFormat, value);
            }
        }

        public string FormatForGenerateText
        {
            get
            {
                return this.formatForGenerateText;
            }
            set
            {
                this.SetPropNotify(ref this.formatForGenerateText, value);
            }
        }

        public bool ExportInMainControlsChildExports
        {
            get
            {
                return this.exportInMainControlsChildExportsInRow;
            }
            set
            {
                this.SetPropNotify(ref this.exportInMainControlsChildExportsInRow, value);
            }
        }

        public bool CaseOfCopiedAlgNameFollowsOutputType
        {
            get
            {
                return this.caseOfCopiedAlgNameFollowsOutputType;
            }
            set
            {
                this.SetPropNotify(ref this.caseOfCopiedAlgNameFollowsOutputType, value);
            }
        }

        public ObservableCollection<TemplateForExportModel> TemplatesForExport
        {
            get
            {
                return this.templatesForExport;
            }
            set
            {
                this.SetPropNotify(ref this.templatesForExport, value);
            }
        }

        public ObservableCollection<TemplateForChecklistModel> TemplatesForChecklist
        {
            get
            {
                return this.templatesForChecklist;
            }
            set
            {
                this.SetPropNotify(ref this.templatesForChecklist, value);
            }
        }

        [JsonIgnore, XmlIgnore]
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
                SettingsPanel.This, "安装外壳扩展可能需要重启资源管理器，确定现在安装吗？", "询问",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.InstallShellExtension() is Exception exception1)
            {
                MessageBox.Show(SettingsPanel.This, exception1.Message, "安装外壳扩展失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(SettingsPanel.This, $"安装外壳扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            if (!File.Exists(Settings.MenuConfigUnicode))
            {
                if (File.Exists(Settings.MenuConfigFile))
                {
                    string reason = ShellMenuEditorModel.AnsiMenuConfigToUnicodeMenuConfig();
                    if (string.IsNullOrEmpty(reason))
                    {
                        goto FinalizeAndReturn;
                    }
                    MessageBox.Show(SettingsPanel.This,
                        $"无法将旧版快捷菜单配置文件转换为新版，将恢复为默认配置。\n{reason}", "警告",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                string exception2 = new ShellMenuEditorModel(SettingsPanel.This).SaveMenuListToJsonFile();
                if (!string.IsNullOrEmpty(exception2))
                {
                    MessageBox.Show(SettingsPanel.This,
                        $"外壳扩展模块配置文件创建失败，快捷菜单可能无法显示，原因：{exception2}", "警告",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        FinalizeAndReturn:
            this.NotSettingShellExtension = true;
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
                SettingsPanel.This, "卸载外壳扩展可能需要重启资源管理器，确定现在卸载吗？", "询问",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                == MessageBoxResult.No)
            {
                return;
            }
            this.NotSettingShellExtension = false;
            if (await ShellExtHelper.UninstallShellExtension() is Exception exception)
            {
                MessageBox.Show(SettingsPanel.This, exception.Message, "卸载外壳扩展失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(SettingsPanel.This, $"卸载外壳扩展成功！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            this.NotSettingShellExtension = true;
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
            SettingsPanel.This.Close();
            ShellMenuEditor shellextEditor = new ShellMenuEditor()
            {
                Owner = MainWindow.This
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
        public TemplateForExportModel SelectedTemplateForExport
        {
            get
            {
                return this.selectedExportTemplate;
            }
            set
            {
                this.SetPropNotify(ref this.selectedExportTemplate, value);
            }
        }

        [JsonIgnore, XmlIgnore]
        public TemplateForChecklistModel SelectedTemplateForChecklist
        {
            get
            {
                return this.selectedChecklistTemplate;
            }
            set
            {
                this.SetPropNotify(ref this.selectedChecklistTemplate, value);
            }
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
                    MessageBox.Show(SettingsPanel.This, "没有选择任何方案！", "提示", MessageBoxButton.OK,
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
                    MessageBox.Show(SettingsPanel.This, "没有选择任何方案！", "提示", MessageBoxButton.OK,
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
                TemplateForExportModel.AllModel.Copy(null)
            };
        }

        private void ResetExportTemplateAction(object param)
        {
            this.ResetTemplatesForExport();
            MessageBox.Show(SettingsPanel.This, "已重置导出结果方案列表。", "提示",
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
                    MessageBox.Show(SettingsPanel.This, "没有选择任何方案！", "提示", MessageBoxButton.OK,
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
                    MessageBox.Show(SettingsPanel.This, "没有选择任何方案！", "提示", MessageBoxButton.OK,
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
            MessageBox.Show(SettingsPanel.This, "已重置解析检验依据方案列表。", "提示",
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
            this.SelectedAlgos = AlgosPanelModel.ProvidedAlgos.Where(i => i.Selected).Select(
                i => i.AlgoType).ToArray();
        }

        internal void OnSettingsViewModelDeserialized()
        {
            if (this.TemplatesForExport == null || !this.TemplatesForExport.Any())
            {
                this.ResetTemplatesForExport();
            }
            if (this.TemplatesForChecklist == null || !this.TemplatesForChecklist.Any())
            {
                this.ResetTemplatesForChecklist();
            }
            foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
            {
                model.Selected = this.SelectedAlgos?.Any(i => i == model.AlgoType) ?? false;
            }
        }

        [OnDeserialized]
        internal void OnSettingsViewModelDeserialized(StreamingContext context)
        {
            this.OnSettingsViewModelDeserialized();
        }

        [JsonIgnore, XmlIgnore]
        public bool ClipboardUpdatedByMe { get; set; }

        [JsonIgnore, XmlIgnore]
        public static string FixAlgoDlls { get; } = "更新动态链接库";

        [JsonIgnore, XmlIgnore]
        public static string ShellExtDir { get; } = "用户目录";

        [JsonIgnore, XmlIgnore]
        public static string FixExePath { get; } = "修复程序路径";

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
        public GenericItemModel[] AvailableDroppedSearchPolicies { get; } =
        {
            new GenericItemModel("搜索该文件夹的一代子文件", SearchMethod.Children),
            new GenericItemModel("搜索该文件夹的全部子文件", SearchMethod.Descendants),
            new GenericItemModel("不对该文件夹进行搜索操作", SearchMethod.DontSearch),
        };

        [JsonIgnore, XmlIgnore]
        public GenericItemModel[] AvailableQVSearchPolicies { get; } =
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
