using System;
using System.Windows;
using System.Xml.Serialization;

namespace HashCalculator
{
    public class SettingsViewModel : NotifiableModel
    {
        private bool mainWndTopmost = false;
        private string lastUsedPath = string.Empty;
        private double mainWndWidth = 800.0;
        private double mainWndHeight = 600.0;
        private double mainWndTop = double.NaN;
        private double mainWndLeft = double.NaN;
        private double settingsWndWidth = 400.0;
        private double settingsWndHeight = 280.0;
        private AlgoType selectedAlgorithm = AlgoType.SHA1;
        private OutputType selectedOutputType = OutputType.BinaryUpper;
        private TaskNum selectedTaskNumberLimit = TaskNum.Two;
        private bool showResultText = false;
        private bool noExportColumn = false;
        private bool noDurationColumn = false;
        private bool noFileSizeColumn = false;
        private readonly object algoSelectionLock = new object();
        private readonly object outTypeSelectionLock = new object();

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

        public bool RememberMainWndPos { get; set; } = true;

        public bool RememberMainWndSize { get; set; } = true;

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
                lock (this.algoSelectionLock)
                {
                    return this.selectedAlgorithm;
                }
            }
            set
            {
                lock (this.algoSelectionLock)
                {
                    this.selectedAlgorithm = value;
                }
            }
        }

        public OutputType SelectedOutputType
        {
            get
            {
                lock (this.outTypeSelectionLock)
                {
                    return this.selectedOutputType;
                }
            }
            set
            {
                lock (this.outTypeSelectionLock)
                {
                    this.selectedOutputType = value;
                }
            }
        }

        public SearchPolicy SelectedQVSearchPolicy { get; set; }

        public SearchPolicy SelectedDroppedSearchPolicy { get; set; }

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
        public AlgoType[] AvailableAlgos { get; } =
        {
            AlgoType.SHA1,
            AlgoType.SHA224,
            AlgoType.SHA256,
            AlgoType.SHA384,
            AlgoType.SHA512,
            AlgoType.MD5,
        };

        [XmlIgnore]
        public ComboItem[] AvailableOutputTypes { get; } =
{
            new ComboItem("Base64 编码", OutputType.BASE64),
            new ComboItem("十六进制大写", OutputType.BinaryUpper),
            new ComboItem("十六进制小写", OutputType.BinaryLower),
        };

        [XmlIgnore]
        public ComboItem[] AvailableTaskNumLimits { get; } =
{
            new ComboItem("1 个：大多数文件很大", TaskNum.One),
            new ComboItem("2 个：大多数文件较大", TaskNum.Two),
            new ComboItem("4 个：大多数文件较小", TaskNum.Four),
            new ComboItem("8 个：大多数文件很小", TaskNum.Eight),
        };

        [XmlIgnore]
        public string TaskNumLimitsToolTip { get; } =
            "当面板为空时，如果校验依据选择的是通用格式的哈希值文本文件，则：\n" +
            "点击 [校验] 后程序会自动解析文件并在相同目录下寻找要计算哈希值的文件完成计算并显示校验结果。\n" +
            "通用格式的哈希值文件请参考程序 [导出结果] 功能导出的文件的内容排布格式。";

        [XmlIgnore]
        public ComboItem[] AvailableDroppedSearchPolicies { get; } =
        {
            new ComboItem("搜索一代子文件", SearchPolicy.Children),
            new ComboItem("搜索全部子文件", SearchPolicy.Descendants),
            new ComboItem("不搜索该文件夹", SearchPolicy.DontSearch),
        };

        [XmlIgnore]
        public ComboItem[] AvailableQVSearchPolicies { get; } =
{
            new ComboItem("搜索依据所在目录的一代子文件", SearchPolicy.Children),
            new ComboItem("搜索依据所在目录的所有子文件", SearchPolicy.Descendants),
        };
    }
}
