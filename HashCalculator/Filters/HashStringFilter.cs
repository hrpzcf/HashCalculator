using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class HashStringFilter : AbsHashViewFilter
    {
        public override ContentControl Settings { get; }

        public override string Display => "哈希值";

        public override string Description => "将各行中包含指定哈希值的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get; set; }

        public HashStringFilter()
        {
            this.Settings = new HashStringFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null || !(this.Param is FilterLogic filterLogic) ||
                !(this.Items is string[] hashStrings))
            {
                return;
            }
            HashSet<byte[]> expectedHashs = hashStrings.Select(i => CommonUtils.HashFromAnyString(i)).Where(
                i => i != null).ToHashSet<byte[]>(BytesComparer.Default);
            if (expectedHashs.Any())
            {
                foreach (HashViewModel model in models)
                {
                    if (!model.Matched)
                    {
                        continue;
                    }
                    if (model.AlgoInOutModels == null)
                    {
                        model.Matched = false;
                    }
                    else
                    {
                        HashSet<byte[]> modelHashs = model.AlgoInOutModels.Select(i => i.HashResult).ToHashSet<byte[]>(BytesComparer.Default);
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelHashs.Overlaps(expectedHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelHashs.SetEquals(expectedHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelHashs.IsSubsetOf(expectedHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelHashs.IsSupersetOf(expectedHashs))
                            {
                                model.Matched = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
