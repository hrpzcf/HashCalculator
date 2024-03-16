using System.IO;
using System.Text;

namespace HashCalculator
{
    public class TemplateForExportModel
    {
        private string extension;
        private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

        public static readonly TemplateForExportModel AllModel =
            new TemplateForExportModel(
                "所有文件",
                null,
                "$hash$ *$relpath$");

        public static readonly TemplateForExportModel CsvModel =
            new TemplateForExportModel(
                "CSV文件",
                ".csv",
                "$algo$,$hash$,$relpath$")
            {
                UsingEncoding = EncodingEnum.ANSI,
            };

        public static readonly TemplateForExportModel HcbModel =
            new TemplateForExportModel(
                "校验依据",
                ".hcb",
                "#$algo$ *$hash$ *$relpath$");

        public static readonly TemplateForExportModel SfvModel =
            new TemplateForExportModel(
                "SFV文件",
                ".sfv",
                "$relpath$ $hash$");

        public static readonly TemplateForExportModel TxtModel =
            new TemplateForExportModel(
                "文本文件",
                ".txt",
                "#$algo$ *$hash$ *$relpath$");

        public TemplateForExportModel() { }

        public TemplateForExportModel(string name, string extension, string template)
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
            }
        }

        public string Template { get; set; }

        public EncodingEnum UsingEncoding { get; set; }

        public string GetFilterFormat()
        {
            if (this.IsValidProps())
            {
                if (string.IsNullOrEmpty(this.Extension))
                {
                    return $"{this.Name}|*.*";
                }
                else if (this.Extension.IndexOfAny(invalidChars) == -1)
                {
                    return $"{this.Name}|*{this.Extension}";
                }
            }
            return default(string);
        }

        public bool IsValidProps()
        {
            return !string.IsNullOrEmpty(this.Name) && !string.IsNullOrEmpty(this.Template);
        }

        public TemplateForExportModel Copy(string nameSuffix)
        {
            if (string.IsNullOrEmpty(nameSuffix))
            {
                return new TemplateForExportModel(this.Name, this.Extension, this.Template)
                {
                    UsingEncoding = this.UsingEncoding
                };
            }
            else
            {
                return new TemplateForExportModel(this.Name + nameSuffix, this.Extension, this.Template)
                {
                    UsingEncoding = this.UsingEncoding
                };
            }
        }

        public Encoding GetEncoding()
        {
            switch (this.UsingEncoding)
            {
                default:
                case EncodingEnum.UTF_8:
                    return new UTF8Encoding();
                case EncodingEnum.UTF_8_BOM:
                    return new UTF8Encoding(true);
                case EncodingEnum.ANSI:
                    return Encoding.Default;
                case EncodingEnum.UNICODE:
                    return new UnicodeEncoding();
            }
        }

        public static GenericItemModel[] AvailableEncodings { get; } =
            new GenericItemModel[]
            {
                new GenericItemModel("UTF-8", EncodingEnum.UTF_8),
                new GenericItemModel("带 BOM 的 UTF-8", EncodingEnum.UTF_8_BOM),
                new GenericItemModel("ANSI", EncodingEnum.ANSI),
                new GenericItemModel("UNICODE", EncodingEnum.UNICODE),
            };
    }
}
