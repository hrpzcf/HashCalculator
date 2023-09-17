using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class DistinctFilesFilter : HashViewFilter
    {
        public override string Display => "有效的文件";

        public override string Description => "过滤重复计算和不存在的文件，重复文件只显示其中一行";

        public override object Param { get; set; }

        public override object[] Items { get; set; }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null)
            {
                foreach (HashViewModel model in models)
                {
                    if (!model.Matched)
                    {
                        continue;
                    }
                    model.FileIndex = model.FileInfo.FullName.GetFileIndex();
                    if (model.FileIndex == null)
                    {
                        model.Matched = false;
                    }
                }
                IEnumerable<IGrouping<CmpableFileIndex, HashViewModel>> groupByFileIndex =
                    models.Where(i => i.FileIndex != null).GroupBy(i => i.FileIndex);
                foreach (IGrouping<CmpableFileIndex, HashViewModel> group in groupByFileIndex)
                {
                    foreach (HashViewModel model in group.Skip(1))
                    {
                        model.Matched = false;
                        model.FileIndex = null;
                    }
                }
            }
        }
    }
}
