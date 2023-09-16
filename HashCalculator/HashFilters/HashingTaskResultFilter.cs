using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class HashingTaskResultFilter : HashViewFilter
    {
        private readonly ControlItem[] expResultCtrls = new ControlItem[]
        {
            new ControlItem("已取消", HashResult.Canceled),
            new ControlItem("已失败", HashResult.Failed),
            new ControlItem("已成功", HashResult.Succeeded),
        };

        public override string Display => "运行结果";

        public override string Description => "将各行中符合指定运行结果的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items
        {
            get => this.expResultCtrls;
            set { }
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null)
            {
                return;
            }
            HashSet<HashResult> expectedResults = this.expResultCtrls.Where(i => i.Selected).Select(i => (HashResult)i.ItemValue).ToHashSet();
            if (expectedResults.Any())
            {
                foreach (HashViewModel model in models)
                {
                    if (model.Matched && !expectedResults.Any(i => model.Result == i))
                    {
                        model.Matched = false;
                    }
                }
            }
        }
    }
}
