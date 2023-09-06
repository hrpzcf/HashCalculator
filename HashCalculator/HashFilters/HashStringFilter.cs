using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class HashStringFilter : HashViewFilter<HashViewModel>
    {
        public override string Display => "哈希值";

        public override string Description => "将各行中包含指定哈希值的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get; set; }

        public override void SetFilterTags(HashViewModel model)
        {
            if (model != null && this.Param is FilterLogic filterLogic && this.Items is string[] hashStrings)
            {
                if (hashStrings.Any())
                {
                    if (model.AlgoInOutModels == null)
                    {
                        model.Matched = false;
                    }
                    else
                    {
                        HashBytesComparer comparer = new HashBytesComparer();
                        HashSet<byte[]> modelHashs = model.AlgoInOutModels.Select(i => i.HashResult).ToHashSet(comparer);
                        HashSet<byte[]> expHashs = hashStrings.Select(i => CommonUtils.HashFromAnyString(i)).ToHashSet(comparer);
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelHashs.Overlaps(expHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelHashs.SetEquals(expHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelHashs.IsSubsetOf(expHashs))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelHashs.IsSupersetOf(expHashs))
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
