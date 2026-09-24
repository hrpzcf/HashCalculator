using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace HashCalculator;

public class TemplateForChecklistModel
{
    public static readonly TemplateForChecklistModel TxtFile =
        new TemplateForChecklistModel()
        {
            Name = "文本文件",
            Extension = ".txt",
            Template = "^#$algo$ \\*?$hash$ \\*?$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel CsvFile =
        new TemplateForChecklistModel()
        {
            Name = "CSV文件",
            Extension = ".csv",
            Template = "^$algo$,$hash$,$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel HcbFile =
        new TemplateForChecklistModel()
        {
            Name = "校验信息",
            Extension = ".hcb",
            Template = "^#$algo$ \\*?$hash$ \\*?$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel SumsFile =
        new TemplateForChecklistModel()
        {
            Name = "SUMS文件",
            Extension = ".sums",
            Template = "^#(?<algo>[A-Za-z0-9( )-]+)#.*\\r?\\n$hash$ \\*?$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel HashFile =
        new TemplateForChecklistModel()
        {
            Name = "HASH文件",
            Extension = ".hash",
            Template = "^#(?<algo>[A-Za-z0-9( )-]+)#.*\\r?\\n$hash$ \\*?$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel SfvFile =
        new TemplateForChecklistModel()
        {
            Name = "SFV文件",
            Extension = ".sfv",
            Template = "^(?!;)$name$ $hash$\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile1 =
        new TemplateForChecklistModel()
        {
            Name = "通用一",
            Extension = null,
            Template = "^\\s*$hash$\\s*\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile2 =
        new TemplateForChecklistModel()
        {
            Name = "通用二",
            Extension = null,
            Template = "^\\s*$hash$\\s+\\*?$name$\\s*\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile3 =
        new TemplateForChecklistModel()
        {
            Name = "通用三",
            Extension = null,
            Template = "^\\s*$name$\\s+$hash$\\s*\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile4 =
        new TemplateForChecklistModel()
        {
            Name = "通用四",
            Extension = null,
            Template = "^#$algo$\\s\\*?$hash$\\s\\*?$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile5 =
        new TemplateForChecklistModel()
        {
            Name = "通用五",
            Extension = null,
            Template = "^$algo$:$hash$\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile6 =
        new TemplateForChecklistModel()
        {
            Name = "通用六",
            Extension = null,
            Template = "^$hash$\\|$name$\\r?$"
        };

    public static readonly TemplateForChecklistModel AnyFile7 =
        new TemplateForChecklistModel()
        {
            Name = "通用七",
            Extension = null,
            Template = "^$hash$\\|\\d+\\|$name$\\r?$"
        };

    private const string algoGroup = "algo";
    private const string hashGroup = "hash";
    private const string nameGroup = "name";
    private Regex regex = null;
    private string extension = null;
    private string template = null;
    private bool propertyChanged = true;
    private static readonly ReadOnlyDictionary<string, string> namedGroupMap =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>()
            {
                {$"${algoGroup}$", $"(?<{algoGroup}>[A-Za-z0-9-]+)" },
                {$"${nameGroup}$", $"(?<{nameGroup}>[^:*?\"<>|\t\v\f\r\n]+)" },
                {$"${hashGroup}$", $"(?<{hashGroup}>[A-Za-z0-9+/=]+)" },
            }
        );

    public TemplateForChecklistModel() { }

    public TemplateForChecklistModel(string name, string extension, string template)
    {
        this.Name = name;
        this.Extension = extension;
        this.Template = template;
    }

    public string Name { get; set; }

    public string Extension
    {
        get
        {
            return this.extension;
        }
        set
        {
            this.extension = value;
            this.propertyChanged = true;
        }
    }

    public string Template
    {
        get
        {
            return this.template;
        }
        set
        {
            this.template = value;
            this.propertyChanged = true;
        }
    }

    private bool InitializeRegex()
    {
        if (!string.IsNullOrEmpty(this.Template))
        {
            if (this.propertyChanged || this.regex == null)
            {
                StringBuilder stringBuilder = new StringBuilder(this.Template);
                foreach (KeyValuePair<string, string> p in namedGroupMap)
                {
                    stringBuilder.Replace(p.Key, p.Value);
                }
                try
                {
                    // 构造一次供整个校验信息文件复用，不再依赖静态 Regex 缓存
                    this.regex = new Regex(
                        stringBuilder.ToString(), RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                }
                catch (Exception)
                {
                    // 模板不合法（如自定义正则语法错误）时视为没有可用模式
                    this.regex = null;
                }
            }
            // 模板或扩展名未变化则不再重建，否则每次匹配都要重新构造 Regex
            this.propertyChanged = false;
            return this.regex != null;
        }
        return false;
    }

    public bool ContainsExtension(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
        {
            return string.IsNullOrEmpty(this.extension);
        }
        return this.extension?.IndexOf(fileExtension, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public TemplateForChecklistModel Copy(string nameSuffix)
    {
        string newName = this.Name;
        if (!string.IsNullOrEmpty(nameSuffix))
        {
            newName += nameSuffix;
        }
        return new TemplateForChecklistModel(newName, this.Extension, this.Template);
    }

    internal bool ExtendChecklistWithLines(string lines, HashChecklist checklist)
    {
        int checkItemCount = 0;
        if (!string.IsNullOrEmpty(lines) && checklist != null && this.InitializeRegex())
        {
            try
            {
                // 用 Match/NextMatch 逐个前进：任意时刻只有一个 Match 存活，不像 Regex.Matches
                // 那样把整份匹配结果（大型校验信息文件可达数十万个 Match）保留到枚举结束
                for (Match match = this.regex.Match(lines); match.Success; match = match.NextMatch())
                {
                    if (checklist.AddChecklistItem(match.Groups[algoGroup].Value, match.Groups[hashGroup].Value,
                        match.Groups[nameGroup].Value))
                    {
                        ++checkItemCount;
                    }
                }
            }
            catch (Exception) { }
        }
        return checkItemCount != 0;
    }
}
