using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace HashCalculator
{
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
                Name = "校验依据",
                Extension = ".hcb",
                Template = "^#$algo$ \\*?$hash$ \\*?$name$\\r?$"
            };

        public static readonly TemplateForChecklistModel SumsFile =
            new TemplateForChecklistModel()
            {
                Name = "SUMS文件",
                Extension = ".sums",
                Template = "^#$algo$#.*\\r?\\n$hash$ \\*?$name$\\r?$"
            };

        public static readonly TemplateForChecklistModel HashFile =
            new TemplateForChecklistModel()
            {
                Name = "HASH文件",
                Extension = ".hash",
                Template = "^#$algo$#.*\\r?\\n$hash$ \\*?$name$\\r?$"
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
                Template = "^$hash$\\r?$"
            };

        public static readonly TemplateForChecklistModel AnyFile2 =
            new TemplateForChecklistModel()
            {
                Name = "通用二",
                Extension = null,
                Template = "^$hash$ \\*?$name$\\r?$"
            };

        public static readonly TemplateForChecklistModel AnyFile3 =
            new TemplateForChecklistModel()
            {
                Name = "通用三",
                Extension = null,
                Template = "^#$algo$ \\*?$hash$ \\*?$name$\\r?$"
            };

        private const string algoGroupName = "algo";
        private const string hashGroupName = "hash";
        private const string nameGroupName = "name";
        private static readonly RegexOptions defaultOptions =
            RegexOptions.ExplicitCapture | RegexOptions.Multiline;
        private string extension = null;
        private string template = null;
        private string regexPattern = null;
        private bool propertyChanged = true;
        private static readonly ReadOnlyDictionary<string, string> namedGroupMap =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>()
                {
                    {"$algo$", "(?<algo>[A-Za-z0-9-]+)" },
                    {"$hash$", "(?<hash>[A-Za-z0-9+/=]+)" },
                    {"$name$", "(?<name>[^:*?\"<>|\r\n]+)" },
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
                return string.IsNullOrEmpty(this.extension) ? string.Empty :
                    this.extension[0] == '.' ? this.extension : $".{this.extension}";
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

        private bool InitializePattern()
        {
            if (!string.IsNullOrEmpty(this.Template))
            {
                if (this.propertyChanged || string.IsNullOrEmpty(this.regexPattern))
                {
                    var stringBuilder = new StringBuilder(this.Template);
                    foreach (KeyValuePair<string, string> p in namedGroupMap)
                    {
                        stringBuilder.Replace(p.Key, p.Value);
                    }
                    this.regexPattern = stringBuilder.ToString();
                }
                return !string.IsNullOrEmpty(this.regexPattern);
            }
            return false;
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
            bool result = false;
            if (!string.IsNullOrEmpty(lines) && checklist != null && this.InitializePattern())
            {
                try
                {
                    MatchCollection matches = Regex.Matches(lines, this.regexPattern, defaultOptions);
                    foreach (Match match in matches)
                    {
#if DEBUG
                        string algo = match.Groups[algoGroupName].Value;
                        string hash = match.Groups[hashGroupName].Value;
                        string name = match.Groups[nameGroupName].Value;
                        result = checklist.AddCheckItem(algo, hash, name);
                        Console.WriteLine($"algo: {algo}; hash: {hash}; name: {name}");
#else
                        result = checklist.AddCheckItem(match.Groups[algoGroupName].Value, match.Groups[hashGroupName].Value,
                            match.Groups[nameGroupName].Value);
#endif
                    }
                }
                catch (Exception) { }
            }
            return result;
        }
    }
}
