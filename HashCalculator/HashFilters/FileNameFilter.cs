using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HashCalculator
{
    internal class FileNameFilter : HashViewFilter
    {
        public override string Display => "搜索文件名";

        public override string Description => "筛选出含有指定字符串或与指定正则表达式匹配的文件名";

        public string Pattern { get; set; }

        public override object Param { get; set; } = false;

        public override object[] Items { get; set; }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && !string.IsNullOrEmpty(this.Pattern) && this.Param is bool regexp)
            {
                if (regexp)
                {
                    Regex regex = new Regex(this.Pattern);
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
                    foreach (HashViewModel model in models)
                    {
                        if (!model.FileName.Contains(this.Pattern))
                        {
                            model.Matched = false;
                        }
                    }
                }
            }
        }
    }
}
