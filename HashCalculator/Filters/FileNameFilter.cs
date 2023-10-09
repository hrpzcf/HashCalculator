using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class FileNameFilter : AbsHashViewFilter
    {
        public override ContentControl Settings { get; }

        public override string Display => "搜索文件名";

        public override string Description => "筛选出含有指定字符串或与指定正则表达式匹配的文件名";

        public string Pattern { get; set; }

        public bool IgnoreCase { get; set; }

        /// <summary>
        /// 是否使用正则表达式
        /// </summary>
        public override object Param { get; set; } = false;

        public override object[] Items { get; set; }

        public FileNameFilter()
        {
            this.Settings = new FileNameFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && !string.IsNullOrEmpty(this.Pattern) && this.Param is bool regexp)
            {
                if (regexp)
                {
                    RegexOptions options = this.IgnoreCase ? RegexOptions.IgnoreCase :
                        RegexOptions.None;
                    Regex regex = new Regex(this.Pattern, options);
                    foreach (HashViewModel model in models)
                    {
                        if (!regex.IsMatch(model.FileName))
                        {
                            model.Matched = false;
                        }
                    }
                }
                else
                {
                    StringComparison comparison = this.IgnoreCase ? StringComparison.OrdinalIgnoreCase :
                        StringComparison.Ordinal;
                    foreach (HashViewModel model in models)
                    {
                        if (model.FileName.IndexOf(this.Pattern, comparison) < 0)
                        {
                            model.Matched = false;
                        }
                    }
                }
            }
        }
    }
}
