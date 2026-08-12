using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using HashCalculator.ViewModels.UserControls;
using HashCalculator.Views.Windows;
using Newtonsoft.Json;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace HashCalculator.ViewModels.Pages;

public class SettingsViewModel : BaseViewModel
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
    private double shellMenuEditorWidth = 600.0;
    private double shellMenuEditorHeight = 400.0;
    private double shellSubmenuEditorWidth = 400.0;
    private double shellSubmenuEditorHeight = 600.0;
    private double exceptionWindowWidth = 800.0;
    private double exceptionWindowHeight = 600.0;
    private double filterOperationWindowTop = double.NaN;
    private double filterOperationWindowLeft = double.NaN;
    private double filterOperationWindowWidth = 800;
    private double filterOperationWindowHeight = 600;

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
    private bool displayMainWindowButtons = true;
    private bool displayMainWndButtonText = true;
    private bool useDefaultOutputTypeWhenExporting = true;
    private bool useExistingClipboardTextForCheck = false;
    private bool monitorNewHashStringInClipboard = true;
    private bool switchMainWndFgWhenNewHashCopied = true;
    private bool caseOfCopiedAlgNameFollowsOutputType = false;
    private bool exportInMainControlsChildExportsInRow = false;
    private bool useUnixStyleLineBreaks = true;
    private bool eachAlgoExportedToSeparateFile = false;
    private bool askUserHowToExportResultsEveryTime = true;
    private bool delayTheStartOfCalculationTasks = false;
    private bool markTheSameHashValues = false;
    private bool automaticallyStartTaskAfterFileAdded = true;
    private bool clearTableBeforeAddingFilesByCmdLine = false;
    private bool clearSelectedItemsAfterCompletion = false;
    private bool isMainWindowNavigationViewPaneOpen = true;
    private bool moveFilesToRecycleBinSilently = false;

    // 主窗口顶部各按钮的显示与否
    private bool showSelectedOutputTypeButton = true;
    private bool showApplyRefreshFilterslButton = true;
    private bool showSelectFilesToHashButton = true;
    private bool showSelectFoldersToHashButton = true;
    private bool showStopEnumeratingPackageButton = true;
    private bool showExportHashResultsButton = true;
    private bool showCopyAndRestartModelsButton = true;
    private bool showRefreshOriginalModelsButton = true;
    private bool showForceRefreshOriginalModelsButton = true;
    private bool showClearAllTableLinesButton = true;
    private bool showPauseDisplayedModelsButton = true;
    private bool showCancelDisplayedModelsButton = true;
    private bool showContinueDisplayedModelsButton = true;
    private bool showMainWindowTopmostButton = true;
    private bool showSeparatorBesideFuncButtons1 = true;
    private bool showSeparatorBesideFuncButtons2 = true;
    private bool showSeparatorBesideFuncButtons3 = true;
    private bool showSeparatorBesideFuncButtons4 = true;

    private CmpRes algoToSwitchToAfterHashChecked = CmpRes.Matched;
    private MenuType selectionWhenNoVerbIsSpecified = MenuType.Compute;
    private ExportAlgo howToExportHashValues = ExportAlgo.AllCalculated;
    private FetchAlgoOption fetchAlgorithmOption = FetchAlgoOption.TATSAMSHDL;
    private OutputType selectedOutputType = OutputType.BinaryUpper;
    private WindowState mainWindowState = WindowState.Normal;
    private WindowState exceptionWindowState = WindowState.Normal;
    private ConfigLocation locationForSavingConfigFiles = ConfigLocation.Unset;

    private string lastUsedPath = string.Empty;
    private string displayingActiveConfigDir = null;
    private string displayingShellExtensionDir = null;
    private string displayingShellInstallationScope = null;
    private string displayingShellInstallationState = null;
    private string formatForGenerateText = "#$algo$ *$hash$ *$name$";
    private string serialColumnLeftDoubleClick = string.Empty;
    private string fileNameColumnLeftDoubleClick = string.Empty;
    private string fullPathColumnLeftDoubleClick = string.Empty;
    private string fileSizeColumnLeftDoubleClick = string.Empty;
    private string hashValueColumnLeftDoubleClick = CmdStrShowDetails;
    private string durationColumnLeftDoubleClick = string.Empty;

    private ShellMenuEditorModel loadedShellMenuEditorModel = null;
    private AlgoInOutModel selectedAlgoInOutModel = AlgorithmsModel.ProvidedAlgos[0];
    private TemplateForExportModel selectedExportTemplate;
    private TemplateForChecklistModel selectedChecklistTemplate;
    private ObservableCollection<TemplateForExportModel> templatesForExport = null;
    private ObservableCollection<TemplateForChecklistModel> templatesForChecklist = null;

    private int selectedTaskNumberLimit = 1;
    private int minCopiedCharsToTriggerHashCheck = 8;
    private int maxCopiedCharsToTriggerHashCheck = 512;
    private int millisecondsOfDelayedStartup = 360;
    private int luminanceOfTableRowsWithSameHash = 100;
    private int saturationOfTableRowsWithSameHash = 240;
    private int luminanceOfTableCellsWithSameDirectory = 100;
    private int saturationOfTableCellsWithSameDirectory = 240;
    private int luminanceOfTableCellsWithSameHash = 100;
    private int saturationOfTableCellsWithSameHash = 240;
    private int addHashViewModelsBatchSize = 0;
    private int selectedApplicationThemeIndex = 0;

    private long snackbarNotificationTimeSpanSeconds = 2;

    private RelayCommand installShellExtCmd;
    private RelayCommand unInstallShellExtCmd;
    private RelayCommand loadContextMenuConfigCmd;
    private RelayCommand saveContextMenuConfigCmd;
    private RelayCommand cancelContextMenuConfigCmd;
    private RelayCommand resetContextMenuConfigCmd;

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
    private RelayCommand resetLuminanceAndSaturationValuesCmd;

    private RelayCommand copyTemplatePlaceholderCmd;
    private RelayCommand openBrowserNavigateToWebsiteCmd;
    private RelayCommand settingsPagesInputBindingsCmd;

    [JsonIgnore, XmlIgnore]
    public const string CmdStrShowDetails = "SHOW_DETAILS";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrOpenFile = "OPEN_FILE";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrExploreFile = "EXPLORE_FILE";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrShowFileProperties = "SHOW_FILE_PROPERTIES";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyCurHash = "COPY_CUR_HASH";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyAllHash = "COPY_ALL_HASH";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyCurHashByTemplate = "COPY_CUR_HASH_BY_TEMPLATE";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyAllHashByTemplate = "COPY_ALL_HASH_BY_TEMPLATE";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyFileName = "COPY_FILE_NAME";

    [JsonIgnore, XmlIgnore]
    public const string CmdStrCopyFilePath = "COPY_FILE_PATH";

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

    public double DpiScaleX => 1.0 / CommonUtils.GetScreenScalingFactor();

    public double DpiScaleY => 1.0 / CommonUtils.GetScreenScalingFactor();

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

    public double ExceptionWindowWidth
    {
        get => this.exceptionWindowWidth;
        set
        {
            if (this.ExceptionWindowState == WindowState.Normal)
            {
                this.SetPropNotify(ref this.exceptionWindowWidth, value);
            }
        }
    }

    public double ExceptionWindowHeight
    {
        get => this.exceptionWindowHeight;
        set
        {
            if (this.ExceptionWindowState == WindowState.Normal)
            {
                this.SetPropNotify(ref this.exceptionWindowHeight, value);
            }
        }
    }

    public WindowState ExceptionWindowState
    {
        get => this.exceptionWindowState;
        set => this.SetPropNotify(ref this.exceptionWindowState, value);
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

    public double FilterOperationWindowTop
    {
        get => this.filterOperationWindowTop;
        set => this.SetPropNotify(ref this.filterOperationWindowTop, value);
    }

    public double FilterOperationWindowLeft
    {
        get => this.filterOperationWindowLeft;
        set => this.SetPropNotify(ref this.filterOperationWindowLeft, value);
    }

    public double FilterOperationWindowWidth
    {
        get => this.filterOperationWindowWidth;
        set => this.SetPropNotify(ref this.filterOperationWindowWidth, value);
    }

    public double FilterOperationWindowHeight
    {
        get => this.filterOperationWindowHeight;
        set => this.SetPropNotify(ref this.filterOperationWindowHeight, value);
    }

    public Dictionary<string, ColumnProperty> ColumnsOrder { get; } =
         new Dictionary<string, ColumnProperty>();

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
        get => this.selectedTaskNumberLimit;
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

    public bool AutomaticallyStartTaskAfterFileAdded
    {
        get => this.automaticallyStartTaskAfterFileAdded;
        set => this.SetPropNotify(ref this.automaticallyStartTaskAfterFileAdded, value);
    }

    public bool ClearSelectedItemsAfterCompletion
    {
        get => this.clearSelectedItemsAfterCompletion;
        set => this.SetPropNotify(ref this.clearSelectedItemsAfterCompletion, value);
    }

    public bool IsMainWindowNavigationViewPaneOpen
    {
        get => this.isMainWindowNavigationViewPaneOpen;
        set => this.SetPropNotify(ref this.isMainWindowNavigationViewPaneOpen, value);
    }

    public bool MoveFilesToRecycleBinSilently
    {
        get => this.moveFilesToRecycleBinSilently;
        set => this.SetPropNotify(ref this.moveFilesToRecycleBinSilently, value);
    }

    public bool ClearTableBeforeAddingFilesByCmdLine
    {
        get => this.clearTableBeforeAddingFilesByCmdLine;
        set => this.SetPropNotify(ref this.clearTableBeforeAddingFilesByCmdLine, value);
    }

    public bool ShowSelectedOutputTypeButton
    {
        get => this.showSelectedOutputTypeButton;
        set => this.SetPropNotify(ref this.showSelectedOutputTypeButton, value);
    }

    public bool ShowApplyRefreshFilterslButton
    {
        get => this.showApplyRefreshFilterslButton;
        set => this.SetPropNotify(ref this.showApplyRefreshFilterslButton, value);
    }

    public bool ShowSelectFilesToHashButton
    {
        get => this.showSelectFilesToHashButton;
        set => this.SetPropNotify(ref this.showSelectFilesToHashButton, value);
    }

    public bool ShowSelectFoldersToHashButton
    {
        get => this.showSelectFoldersToHashButton;
        set => this.SetPropNotify(ref this.showSelectFoldersToHashButton, value);
    }

    public bool ShowStopEnumeratingPackageButton
    {
        get => this.showStopEnumeratingPackageButton;
        set => this.SetPropNotify(ref this.showStopEnumeratingPackageButton, value);
    }

    public bool ShowExportHashResultsButton
    {
        get => this.showExportHashResultsButton;
        set => this.SetPropNotify(ref this.showExportHashResultsButton, value);
    }

    public bool ShowCopyAndRestartModelsButton
    {
        get => this.showCopyAndRestartModelsButton;
        set => this.SetPropNotify(ref this.showCopyAndRestartModelsButton, value);
    }

    public bool ShowRefreshOriginalModelsButton
    {
        get => this.showRefreshOriginalModelsButton;
        set => this.SetPropNotify(ref this.showRefreshOriginalModelsButton, value);
    }

    public bool ShowForceRefreshOriginalModelsButton
    {
        get => this.showForceRefreshOriginalModelsButton;
        set => this.SetPropNotify(ref this.showForceRefreshOriginalModelsButton, value);
    }

    public bool ShowClearAllTableLinesButton
    {
        get => this.showClearAllTableLinesButton;
        set => this.SetPropNotify(ref this.showClearAllTableLinesButton, value);
    }

    public bool ShowPauseDisplayedModelsButton
    {
        get => this.showPauseDisplayedModelsButton;
        set => this.SetPropNotify(ref this.showPauseDisplayedModelsButton, value);
    }

    public bool ShowCancelDisplayedModelsButton
    {
        get => this.showCancelDisplayedModelsButton;
        set => this.SetPropNotify(ref this.showCancelDisplayedModelsButton, value);
    }

    public bool ShowContinueDisplayedModelsButton
    {
        get => this.showContinueDisplayedModelsButton;
        set => this.SetPropNotify(ref this.showContinueDisplayedModelsButton, value);
    }

    public bool ShowMainWindowTopmostButton
    {
        get => this.showMainWindowTopmostButton;
        set => this.SetPropNotify(ref this.showMainWindowTopmostButton, value);
    }

    public bool ShowSeparatorBesideFuncButtons1
    {
        get => this.showSeparatorBesideFuncButtons1;
        set => this.SetPropNotify(ref this.showSeparatorBesideFuncButtons1, value);
    }

    public bool ShowSeparatorBesideFuncButtons2
    {
        get => this.showSeparatorBesideFuncButtons2;
        set => this.SetPropNotify(ref this.showSeparatorBesideFuncButtons2, value);
    }

    public bool ShowSeparatorBesideFuncButtons3
    {
        get => this.showSeparatorBesideFuncButtons3;
        set => this.SetPropNotify(ref this.showSeparatorBesideFuncButtons3, value);
    }

    public bool ShowSeparatorBesideFuncButtons4
    {
        get => this.showSeparatorBesideFuncButtons4;
        set => this.SetPropNotify(ref this.showSeparatorBesideFuncButtons4, value);
    }

    [JsonIgnore, XmlIgnore]
    public bool IsFiltersAndCmdersIdle
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

    [JsonIgnore, XmlIgnore]
    public bool IsMainRowSelectedByCheckBox
    {
        get => this.isMainRowSelectedByCheckBox;
        set => this.SetPropNotify(ref this.isMainRowSelectedByCheckBox, value);
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

        set => this.lastUsedPath = value;
    }

    public bool PreferChecklistAlgs
    {
        get => this.preferChecklistAlgs;
        set => this.SetPropNotify(ref this.preferChecklistAlgs, value);
    }

    public bool ParallelBetweenAlgos
    {
        get => this.parallelBetweenAlgos;
        set
        {
            this.SetPropNotify(ref this.parallelBetweenAlgos, value);
            if (value)
            {
                this.SelectedTaskNumberLimit = 1;
            }
        }
    }

    public bool UseExistingClipboardTextForCheck
    {
        get => this.useExistingClipboardTextForCheck;
        set => this.SetPropNotify(ref this.useExistingClipboardTextForCheck, value);
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

    public bool DisplayMainWindowButtons
    {
        get => this.displayMainWindowButtons;
        set => this.SetPropNotify(ref this.displayMainWindowButtons, value);
    }

    public bool DisplayMainWndButtonText
    {
        get => this.displayMainWndButtonText;
        set => this.SetPropNotify(ref this.displayMainWndButtonText, value);
    }

    public int MinCopiedCharsToTriggerHashCheck
    {
        get => this.minCopiedCharsToTriggerHashCheck;
        set
        {
            if (value > this.MaxCopiedCharsToTriggerHashCheck)
            {
                (value, this.MaxCopiedCharsToTriggerHashCheck) = (this.MaxCopiedCharsToTriggerHashCheck, value);
            }
            this.SetPropNotify(ref this.minCopiedCharsToTriggerHashCheck, value);
        }
    }

    public int MaxCopiedCharsToTriggerHashCheck
    {
        get => this.maxCopiedCharsToTriggerHashCheck;
        set
        {
            if (value < this.MinCopiedCharsToTriggerHashCheck)
            {
                (value, this.MinCopiedCharsToTriggerHashCheck) = (this.MinCopiedCharsToTriggerHashCheck, value);
            }
            this.SetPropNotify(ref this.maxCopiedCharsToTriggerHashCheck, value);
        }
    }

    public CmpRes AlgoToSwitchToAfterHashChecked
    {
        get => this.algoToSwitchToAfterHashChecked;
        set => this.SetPropNotify(ref this.algoToSwitchToAfterHashChecked, value);
    }

    public MenuType SelectionWhenNoVerbIsSpecified
    {
        get => this.selectionWhenNoVerbIsSpecified;
        set => this.SetPropNotify(ref this.selectionWhenNoVerbIsSpecified, value);
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

    private static void AdjustLuminanceOrSaturation(ref int target)
    {
        if (target < 0)
        {
            target = 0;
        }
        else if (target > 240)
        {
            target = 240;
        }
    }

    public int LuminanceOfTableRowsWithSameHash
    {
        get => this.luminanceOfTableRowsWithSameHash;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.luminanceOfTableRowsWithSameHash, value);
        }
    }

    public int SaturationOfTableRowsWithSameHash
    {
        get => this.saturationOfTableRowsWithSameHash;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.saturationOfTableRowsWithSameHash, value);
        }
    }

    public int LuminanceOfTableCellsWithSameDirectory
    {
        get => this.luminanceOfTableCellsWithSameDirectory;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.luminanceOfTableCellsWithSameDirectory, value);
        }
    }

    public int SaturationOfTableCellsWithSameDirectory
    {
        get => this.saturationOfTableCellsWithSameDirectory;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.saturationOfTableCellsWithSameDirectory, value);
        }
    }

    public int LuminanceOfTableCellsWithSameHash
    {
        get => this.luminanceOfTableCellsWithSameHash;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.luminanceOfTableCellsWithSameHash, value);
        }
    }

    public int SaturationOfTableCellsWithSameHash
    {
        get => this.saturationOfTableCellsWithSameHash;
        set
        {
            AdjustLuminanceOrSaturation(ref value);
            this.SetPropNotify(ref this.saturationOfTableCellsWithSameHash, value);
        }
    }

    public int SelectedApplicationThemeIndex
    {
        get => this.selectedApplicationThemeIndex;
        set
        {
            if (MainWindow.Current != null &&
                value == this.selectedApplicationThemeIndex)
            {
                return;
            }
            ApplicationTheme theme = (ApplicationTheme)value;
            if (theme == ApplicationTheme.Unknown)
            {
                SystemThemeWatcher.Watch(MainWindow.Current, WindowBackdropType.None);
            }
            else
            {
                SystemThemeWatcher.UnWatch(MainWindow.Current);
            }
            // 解决 Unknown 或者 value 超出 enum 范围的情况
            theme = theme.InvalidToEffectiveTheme();
            ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
            this.SetPropNotify(ref this.selectedApplicationThemeIndex, value);
        }
    }

    public int AddHashViewModelsBatchSize
    {
        get => this.addHashViewModelsBatchSize;
        set => this.SetPropNotify(ref this.addHashViewModelsBatchSize, value);
    }

    public long SnackbarNotificationTimeSpanSeconds
    {
        get => this.snackbarNotificationTimeSpanSeconds;
        set => this.SetPropNotify(ref this.snackbarNotificationTimeSpanSeconds, value);
    }

    public string SerialColumnLeftDoubleClick
    {
        get => this.serialColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.serialColumnLeftDoubleClick, value);
    }

    public string FileNameColumnLeftDoubleClick
    {
        get => this.fileNameColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.fileNameColumnLeftDoubleClick, value);
    }

    public string FullPathColumnLeftDoubleClick
    {
        get => this.fullPathColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.fullPathColumnLeftDoubleClick, value);
    }

    public string FileSizeColumnLeftDoubleClick
    {
        get => this.fileSizeColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.fileSizeColumnLeftDoubleClick, value);
    }

    public string HashValueColumnLeftDoubleClick
    {
        get => this.hashValueColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.hashValueColumnLeftDoubleClick, value);
    }

    public string DurationColumnLeftDoubleClick
    {
        get => this.durationColumnLeftDoubleClick;
        set => this.SetPropNotify(ref this.durationColumnLeftDoubleClick, value);
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
        if (NotificationSender.ShowMessageBox(
            MainWindow.Current,
            "询问",
            "安装外壳扩展可能需要重启资源管理器，确定现在安装吗？",
            closeButtonText: "否",
            primaryButtonText: "是") != ContentDialogResult.Primary)
        {
            return;
        }
        this.ProcessingShellExtension = true;
        if (await Task.Run(ShellExtHelper.InstallShellExtension) is Exception exception1)
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "安装失败", exception1.Message);
        }
        else
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "安装成功", $"安装外壳扩展成功！");
        }
        if (!File.Exists(Settings.ConfigInfo.MenuConfigFile))
        {
            string exception = new ShellMenuEditorModel().SaveMenuListToJsonFile();
            if (!string.IsNullOrEmpty(exception))
            {
                NotificationSender.ShowMessageBox(MainWindow.Current,
                    "警告", $"外壳扩展模块配置文件创建失败，快捷菜单将不显示，原因：{exception}");
            }
        }
        this.ProcessingShellExtension = false;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand InstallShellExtCmd
    {
        get
        {
            this.installShellExtCmd ??= new RelayCommand(this.InstallShellExtAction);
            return this.installShellExtCmd;
        }
    }

    private async void UnInstallShellExtAction(object param)
    {
        if (NotificationSender.ShowMessageBox(
            MainWindow.Current,
            "询问",
            "卸载外壳扩展可能需要重启资源管理器，确定现在卸载吗？",
            closeButtonText: "否",
            primaryButtonText: "是") != ContentDialogResult.Primary)
        {
            return;
        }
        this.ProcessingShellExtension = true;
        if (await Task.Run(ShellExtHelper.UninstallShellExtension) is Exception exception)
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "卸载失败", exception.Message);
        }
        else
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "卸载成功", $"卸载外壳扩展成功！");
        }
        this.ProcessingShellExtension = false;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand UnInstallShellExtCmd
    {
        get
        {
            this.unInstallShellExtCmd ??= new RelayCommand(this.UnInstallShellExtAction);
            return this.unInstallShellExtCmd;
        }
    }

    [JsonIgnore, XmlIgnore]
    public ShellMenuEditorModel LoadedShellMenuEditorModel
    {
        get => this.loadedShellMenuEditorModel;
        set => this.SetPropNotify(ref this.loadedShellMenuEditorModel, value);
    }

    private void LoadContextMenuConfigAction(object param)
    {
        this.LoadedShellMenuEditorModel = new ShellMenuEditorModel();
    }

    [JsonIgnore, XmlIgnore]
    public ICommand LoadContextMenuConfigCmd
    {
        get
        {
            this.loadContextMenuConfigCmd ??= new RelayCommand(this.LoadContextMenuConfigAction);
            return this.loadContextMenuConfigCmd;
        }
    }

    private void SaveContextMenuConfigAction(object param)
    {
        if (this.LoadedShellMenuEditorModel == null)
        {
            NotificationSender.SnackbarWarning("还没有载入右键菜单配置文件。");
            return;
        }
        string optionException = this.LoadedShellMenuEditorModel.SaveMenuListToJsonFile();
        if (string.IsNullOrEmpty(optionException))
        {
            NotificationSender.SnackbarSuccess("右键菜单配置文件已保存！");
        }
        else
        {
            NotificationSender.SnackbarError($"配置文件保存失败：\n{optionException}");
        }
        this.LoadedShellMenuEditorModel = null;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand SaveContextMenuConfigCmd
    {
        get
        {
            this.saveContextMenuConfigCmd ??= new RelayCommand(this.SaveContextMenuConfigAction);
            return this.saveContextMenuConfigCmd;
        }
    }

    private void CancelContextMenuConfigAction(object param)
    {
        this.LoadedShellMenuEditorModel = null;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand CancelContextMenuConfigCmd
    {
        get
        {
            this.cancelContextMenuConfigCmd ??= new RelayCommand(this.CancelContextMenuConfigAction);
            return this.cancelContextMenuConfigCmd;
        }
    }

    private void ResetContextMenuConfigAction(object param)
    {
        if (this.LoadedShellMenuEditorModel == null)
        {
            NotificationSender.SnackbarWarning("还没有载入右键菜单配置文件。");
            return;
        }
        this.LoadedShellMenuEditorModel.ManuallyResetMenuList();
        NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "右键菜单列表已重置，请手动保存改动！");
    }

    [JsonIgnore, XmlIgnore]
    public ICommand ResetContextMenuConfigCmd
    {
        get
        {
            this.resetContextMenuConfigCmd ??= new RelayCommand(this.ResetContextMenuConfigAction);
            return this.resetContextMenuConfigCmd;
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
        this.TemplatesForExport ??= new ObservableCollection<TemplateForExportModel>();
        this.TemplatesForExport.Add(model);
        this.SelectedTemplateForExport = model;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand AddExportTemplateCmd
    {
        get
        {
            this.addExportTemplateCmd ??= new RelayCommand(this.AddExportTemplateAction);
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
                NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "没有选择任何方案！");
            }
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand CopyExportTemplateCmd
    {
        get
        {
            this.copyExportTemplateCmd ??= new RelayCommand(this.CopyExportTemplateAction);
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
            this.moveExportTemplateUpCmd ??= new RelayCommand(this.MoveExportTemplateUpAction);
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
            this.moveExportTemplateDownCmd ??= new RelayCommand(this.MoveExportTemplateDownAction);
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
                NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "没有选择任何方案！");
            }
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand RemoveExportTemplateCmd
    {
        get
        {
            this.removeExportTemplateCmd ??= new RelayCommand(this.RemoveExportTemplateAction);
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
        NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "已重置结果导出方案列表。");
    }

    [JsonIgnore, XmlIgnore]
    public ICommand ResetExportTemplateCmd
    {
        get
        {
            this.resetExportTemplateCmd ??= new RelayCommand(this.ResetExportTemplateAction);
            return this.resetExportTemplateCmd;
        }
    }

    private void AddChecklistTemplateAction(object param)
    {
        TemplateForChecklistModel model = new TemplateForChecklistModel();
        this.TemplatesForChecklist ??= new ObservableCollection<TemplateForChecklistModel>();
        this.TemplatesForChecklist.Add(model);
        this.SelectedTemplateForChecklist = model;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand AddChecklistTemplateCmd
    {
        get
        {
            this.addChecklistTemplateCmd ??= new RelayCommand(this.AddChecklistTemplateAction);
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
                NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "没有选择任何方案！");
            }
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand CopyChecklistTemplateCmd
    {
        get
        {
            this.copyChecklistTemplateCmd ??= new RelayCommand(this.CopyChecklistTemplateAction);
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
            this.moveChecklistTemplateUpCmd ??= new RelayCommand(this.MoveChecklistTemplateUpAction);
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
            this.moveChecklistTemplateDownCmd ??= new RelayCommand(this.MoveChecklistTemplateDownAction);
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
                NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "没有选择任何方案！");
            }
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand RemoveChecklistTemplateCmd
    {
        get
        {
            this.removeChecklistTemplateCmd ??= new RelayCommand(this.RemoveChecklistTemplateAction);
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
            TemplateForChecklistModel.AnyFile4.Copy(null),
            TemplateForChecklistModel.AnyFile5.Copy(null),
            TemplateForChecklistModel.AnyFile6.Copy(null),
            TemplateForChecklistModel.AnyFile7.Copy(null),
        };
    }

    private void ResetChecklistTemplateAction(object param)
    {
        this.ResetTemplatesForChecklist();
        NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "已重置校验信息解析方案列表。");
    }

    [JsonIgnore, XmlIgnore]
    public ICommand ResetChecklistTemplateCmd
    {
        get
        {
            this.resetChecklistTemplateCmd ??= new RelayCommand(this.ResetChecklistTemplateAction);
            return this.resetChecklistTemplateCmd;
        }
    }

    private void ResetAlgorithmAliasAction(object param)
    {
        foreach (AlgoInOutModel model in AlgorithmsModel.ProvidedAlgos)
        {
            model.ResetAlias();
        }
        NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "已将所有算法的别名恢复到默认状态！");
    }

    [JsonIgnore, XmlIgnore]
    public ICommand ResetAlgorithmAliasCmd
    {
        get
        {
            this.resetAlgorithmAliasCmd ??= new RelayCommand(this.ResetAlgorithmAliasAction);
            return this.resetAlgorithmAliasCmd;
        }
    }

    private void ResetLuminanceAndSaturationValuesAction(object param)
    {
        this.LuminanceOfTableRowsWithSameHash = 100;
        this.SaturationOfTableRowsWithSameHash = 240;

        this.LuminanceOfTableCellsWithSameHash = 100;
        this.SaturationOfTableCellsWithSameHash = 240;

        this.LuminanceOfTableCellsWithSameDirectory = 100;
        this.SaturationOfTableCellsWithSameDirectory = 240;
    }

    [JsonIgnore, XmlIgnore]
    public ICommand ResetLuminanceAndSaturationValuesCmd
    {
        get
        {
            this.resetLuminanceAndSaturationValuesCmd ??= new RelayCommand(this.ResetLuminanceAndSaturationValuesAction);
            return this.resetLuminanceAndSaturationValuesCmd;
        }
    }

    private void OpenBrowserNavigateToWebsiteAction(object param)
    {
        if (param is string url)
        {
            SHELL32.ShellExecuteW(MainWindow.WndHandle, "open", url, null, null, ShowCmd.SW_NORMAL);
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand OpenBrowserNavigateToWebsiteCmd
    {
        get
        {
            this.openBrowserNavigateToWebsiteCmd ??= new RelayCommand(this.OpenBrowserNavigateToWebsiteAction);
            return this.openBrowserNavigateToWebsiteCmd;
        }
    }

    private void SettingsPagesInputBindingsAction(object param)
    {
        if (param is KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape &&
                keyEventArgs.OriginalSource is System.Windows.Controls.DataGrid dataGrid)
            {
                dataGrid.SelectedIndex = -1;
            }
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand SettingsPagesInputBindingsCmd
    {
        get
        {
            this.settingsPagesInputBindingsCmd ??= new RelayCommand(this.SettingsPagesInputBindingsAction);
            return this.settingsPagesInputBindingsCmd;
        }
    }

    private void CopyTemplatePlaceholderAction(object param)
    {
        if (param is string placeholder)
        {
            CommonUtils.ClipboardSetText(placeholder);
            NotificationSender.SnackbarSecondary($"已复制占位符：{placeholder}");
        }
    }

    [JsonIgnore, XmlIgnore]
    public ICommand CopyTemplatePlaceholderCmd
    {
        get
        {
            this.copyTemplatePlaceholderCmd ??= new RelayCommand(this.CopyTemplatePlaceholderAction);
            return this.copyTemplatePlaceholderCmd;
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
        this.AlgorithmAliasList = AlgorithmsModel.ProvidedAlgos.Where(
            i => !string.IsNullOrWhiteSpace(i.AlgorithmAlias)).ToDictionary(
            j => j.AlgoType, k => k.AlgorithmAlias);
        if (this.AlgorithmAliasList.Count == 0)
        {
            // 内容为空，统一设置为 null
            this.AlgorithmAliasList = null;
        }
        this.SelectedAlgos = AlgorithmsModel.ProvidedAlgos.Where(i => i.Selected).Select(
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
            foreach (KeyValuePair<AlgoType, string> keyValuePair in this.AlgorithmAliasList)
            {
                foreach (AlgoInOutModel inOut in AlgorithmsModel.ProvidedAlgos)
                {
                    if (inOut.AlgoType == keyValuePair.Key)
                    {
                        inOut.AlgorithmAlias = keyValuePair.Value;
                        break;
                    }
                }
            }
        }
        foreach (AlgoInOutModel model in AlgorithmsModel.ProvidedAlgos)
        {
            model.Selected = this.SelectedAlgos?.Contains(model.AlgoType) ?? false;
        }
    }

    [JsonIgnore, XmlIgnore]
    public bool ClipboardUpdatedByMe { get; set; }

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

    public static GenericItemModel[] AvailableChoicesWhenNoVerb { get; } =
    {
        new GenericItemModel("计算所有输入文件的哈希值", MenuType.Compute),
        new GenericItemModel("把所有输入文件作为校验信息", MenuType.CheckHash),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableColLeftDoubleClickCmds { get; } =
    {
        new GenericItemModel("未指定", string.Empty),
        new GenericItemModel("打开详情窗口", CmdStrShowDetails),
        new GenericItemModel("打开文件", CmdStrOpenFile),
        new GenericItemModel("打开文件位置", CmdStrExploreFile),
        new GenericItemModel("打开文件属性", CmdStrShowFileProperties),
        new GenericItemModel("复制当前哈希值", CmdStrCopyCurHash),
        new GenericItemModel("复制所有哈希值", CmdStrCopyAllHash),
        new GenericItemModel("按模板复制当前结果", CmdStrCopyCurHashByTemplate),
        new GenericItemModel("按模板复制所有结果", CmdStrCopyAllHashByTemplate),
        new GenericItemModel("复制文件名", CmdStrCopyFileName),
        new GenericItemModel("复制完整路径", CmdStrCopyFilePath),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableDroppedSearchMethods { get; } =
    {
        new GenericItemModel("搜索该文件夹的直属子文件", SearchMethod.Children),
        new GenericItemModel("搜索该文件夹的所有子文件", SearchMethod.Descendants),
        new GenericItemModel("不对该文件夹进行搜索操作", SearchMethod.DontSearch),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableQVSearchMethods { get; } =
    {
        new GenericItemModel("搜索该文件夹的直属子文件", SearchMethod.Children),
        new GenericItemModel("搜索该文件夹的所有子文件", SearchMethod.Descendants),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableFetchAlgoOptions { get; } =
    {
        new GenericItemModel("使用默认算法中被勾选的算法", FetchAlgoOption.SELECTED),
        new GenericItemModel("使用被勾选且可产生相应哈希长度的算法", FetchAlgoOption.TATSAMSHDL),
        new GenericItemModel("使用所有可产生相应哈希长度的算法", FetchAlgoOption.TATMSHDL),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableResultsToSwitchTo { get; } =
    {
        new GenericItemModel("保持现状不执行切换操作", CmpRes.NoResult),
        new GenericItemModel("校验结果是无关联的算法", CmpRes.Unrelated),
        new GenericItemModel("校验结果是已匹配的算法", CmpRes.Matched),
        new GenericItemModel("校验结果是不匹配的算法", CmpRes.Mismatch),
        new GenericItemModel("校验结果是不确定的算法", CmpRes.Uncertain),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailableLocationsForSavingConfigFile { get; } =
    {
        new GenericItemModel("当前目录", ConfigLocation.ExecDir),
        new GenericItemModel("用户目录", ConfigLocation.UserDir),
        new GenericItemModel("公用用户目录", ConfigLocation.PublicUser),
        new GenericItemModel("程序数据目录", ConfigLocation.ProgramData),
    };

    [JsonIgnore, XmlIgnore]
    public string SrcGitee => "https://gitee.com/hrpzcf/HashCalculator";

    [JsonIgnore, XmlIgnore]
    public string SrcGitHub => "https://github.com/hrpzcf/HashCalculator";

    [JsonIgnore, XmlIgnore]
    public string IssueGitee => "https://gitee.com/hrpzcf/HashCalculator/issues";

    [JsonIgnore, XmlIgnore]
    public string IssueGitHub => "https://github.com/hrpzcf/HashCalculator/issues";

    [JsonIgnore, XmlIgnore]
    public string WikiGitee => "https://gitee.com/hrpzcf/HashCalculator/wikis/Home";

    [JsonIgnore, XmlIgnore]
    public string WikiGitHub => "https://github.com/hrpzcf/HashCalculator/wiki";

    [JsonIgnore, XmlIgnore]
    public string ChangeLogGitee => "https://gitee.com/hrpzcf/HashCalculator/blob/main/CHANGELOG.md";

    [JsonIgnore, XmlIgnore]
    public string ChangeLogGitHub => "https://github.com/hrpzcf/HashCalculator/blob/main/CHANGELOG.md";

    [JsonIgnore, XmlIgnore]
    public string Title => Info.Title;

    [JsonIgnore, XmlIgnore]
    public string Author => Info.Author;

    [JsonIgnore, XmlIgnore]
    public string Ver => Info.Ver;

    [JsonIgnore, XmlIgnore]
    public string Website => Info.Website;

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] OpenSourceProjects { get; } = new GenericItemModel[]
    {
        new GenericItemModel(
            "BLAKE2",
            "https://github.com/BLAKE2/BLAKE2",
            "提供 BLAKE2 系列哈希算法的实现。"),
        new GenericItemModel(
            "BLAKE3",
            "https://github.com/BLAKE3-team/BLAKE3",
            "提供 BLAKE3 系列哈希算法的实现。"),
        new GenericItemModel(
            "CRC32",
            "https://github.com/stbrumme/crc32",
            "提供 CRC32 哈希算法的实现。"),
        new GenericItemModel(
            "GmSSL",
            "https://github.com/guanzhi/GmSSL",
            "提供 SM3 哈希算法的实现。"),
        new GenericItemModel(
            "OpenHashTab",
            "https://github.com/namazso/OpenHashTab",
            "提供 CRC64 哈希算法的实现。"),
        new GenericItemModel(
            "QuickXorHash",
            "https://github.com/namazso/QuickXorHash",
            "提供 QuickXor 哈希算法的实现。"),
        new GenericItemModel(
            "RHash",
            "https://github.com/rhash/RHash",
            "提供 eD2k/Has160/MD4/RipeMD160/SHA224/Whirlpool 算法的实现。"),
        new GenericItemModel(
            "Streebog",
            "https://github.com/adegtyarev/streebog",
            "提供 Streebog 系列哈希算法的实现。"),
        new GenericItemModel(
            "XKCP",
            "https://github.com/XKCP/XKCP",
            "提供 SHA3 系列哈希算法的实现。"),
        new GenericItemModel(
            "xxHash",
            "https://github.com/Cyan4973/xxHash",
            "提供 XXH 系列极快速哈希算法的实现。"),
        new GenericItemModel(
            "CommandLine",
            "https://github.com/commandlineparser/commandline",
            "用于解析命令行参数。"),
        new GenericItemModel(
            "WPF UI",
            "https://github.com/lepoco/wpfui",
            "给 HashCalculator 提供 Fluent Design 控件和样式。"),
        new GenericItemModel(
            "Newtonsoft.Json",
            "https://www.newtonsoft.com/json",
            "用于读取和保存本软件的相关配置文件。"),
        new GenericItemModel(
            "Tiny-json",
            "https://github.com/rafagafe/tiny-json",
            "用于读取和保存外壳扩展的相关配置文件。"),
        new GenericItemModel(
            "WindowsAPICodePack",
            "https://github.com/aybe/Windows-API-Code-Pack-1.1",
            "用于调用系统接口打开文件/文件夹选择对话框。"),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailablePlaceholdersForTemplateOfCopyHash { get; } = new GenericItemModel[]
    {
        new GenericItemModel("$algo$", "在复制时此占位符将被替换为实际算法名。"),
        new GenericItemModel("$hash$", "在复制时此占位符将被替换为实际哈希值。"),
        new GenericItemModel("$path$", "在复制时此占位符将被替换为实际文件完整路径。"),
        new GenericItemModel("$relpath$", "此占位符将被替换为实际文件相对路径，起点是被添加的对象所在目录。"),
        new GenericItemModel("$name$", "在复制时此占位符将被替换为实际文件名。"),
        new GenericItemModel("$newline$", "在复制时此占位符将被替换为 Windows 换行符。"),
        new GenericItemModel("$horztab$", "在复制时此占位符将被替换为横向制表符（ \\t ）。"),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailablePlaceholdersForTemplateOfExportHash { get; } = new GenericItemModel[]
    {
        new GenericItemModel("$algo$", "导出计算结果时此占位符将被替换为实际算法名。"),
        new GenericItemModel("$hash$", "导出计算结果时此占位符将被替换为实际哈希值。"),
        new GenericItemModel("$path$", "导出计算结果时此占位符将被替换为实际文件完整路径。"),
        new GenericItemModel("$relpath$", "此占位符将被替换为实际文件相对路径，起点是被添加的对象所在目录。"),
        new GenericItemModel("$name$", "导出计算结果时此占位符将被替换为实际文件名。"),
        new GenericItemModel("$newline$", "导出计算结果时此占位符将被替换为 Unix 或 Windows 换行符。"),
        new GenericItemModel("$horztab$", "导出计算结果时此占位符将被替换为横向制表符（ \\t ）。"),
        new GenericItemModel("$filesize$", "导出计算结果时此占位符将被替换为以“字节”为单位的文件大小值。"),
    };

    [JsonIgnore, XmlIgnore]
    public GenericItemModel[] AvailablePlaceholdersForParsingSchemeExpression { get; } = new GenericItemModel[]
    {
        new GenericItemModel("$algo$", "等同于 (?<algo>[A-Za-z0-9-]+)，此位置的匹配结果作为算法名。"),
        new GenericItemModel("$hash$", "等同于 (?<hash>[A-Za-z0-9+/=]+)，此位置的匹配结果作为哈希值。"),
        new GenericItemModel("$name$", "等同于 (?<name>[^:*?\\\"<>|\\t\\v\\f\\r\\n]+)，此位置的匹配结果作为文件名。"),
    };
}
