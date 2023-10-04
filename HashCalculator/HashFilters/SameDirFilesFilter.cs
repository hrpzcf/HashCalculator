using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HashCalculator
{
    internal class SameDirFilesFilter : HashViewFilter
    {
        public override string Display => "相同文件夹";

        public override string Description => "将各行中位于相同文件夹的文件筛选出来";

        public override object Param { get; set; } = false;

        public override object[] Items { get; set; }

        public override GroupDescription[] GroupDescriptions { get; } =
            new GroupDescription[]
            {
                new PropertyGroupDescription(nameof(HashViewModel.FdGroupId)),
            };

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null)
            {
                Dictionary<string, List<HashViewModel>> groupByDirPath =
                    new Dictionary<string, List<HashViewModel>>(StringComparer.OrdinalIgnoreCase);
                foreach (HashViewModel model in models)
                {
                    if (!model.Matched)
                    {
                        continue;
                    }
                    string dirPath = model.FileInfo.DirectoryName;
                    if (groupByDirPath.ContainsKey(dirPath))
                    {
                        groupByDirPath[dirPath].Add(model);
                    }
                    else
                    {
                        groupByDirPath[dirPath] = new List<HashViewModel> { model };
                    }
                }
                if (groupByDirPath.Any())
                {
                    foreach (var tuple in CommonUtils.ColorGenerator(groupByDirPath.Count).ZipElements(groupByDirPath))
                    {
                        foreach (HashViewModel model in tuple.Item2.Value)
                        {
                            model.FdGroupId = new ComparableColor(tuple.Item1);
                        }
                    }
                }
            }
        }
    }
}
