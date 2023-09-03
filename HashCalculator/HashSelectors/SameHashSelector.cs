using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCalculator
{
    internal class SameHashSelector : HashSelector<IEnumerable<HashViewModel>>
    {
        public override string Display => "相同哈希值";

        public override string Description => "将各行中含有相同哈希值的行筛选出来";

        public override object Param { get; set; }

        public override object[] Items { get; set; }

        public override void SetFilterTags(IEnumerable<HashViewModel> value)
        {
            List<HashViewModel> excludedModles = new List<HashViewModel>();
            Dictionary<byte[], List<HashViewModel>> groupByResult = new Dictionary<byte[], List<HashViewModel>>();
            foreach (HashViewModel item in value)
            {
                if (excludedModles.Contains(item))
                {
                    continue;
                }
                foreach (AlgoInOutModel algo in item.AlgoInOutModels)
                {
                    if (algo.HashResult == null)
                    {
                        if (groupByResult.Keys.Contains(algo.HashResult))
                        {
                            groupByResult[algo.HashResult].Add(item);
                            excludedModles.Add(item);
                        }
                    }
                }
            }
        }
    }
}
