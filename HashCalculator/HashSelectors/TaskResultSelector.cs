using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class TaskResultSelector : HashSelector<HashViewModel>
    {
        public override string Display => "运行结果";

        public override string Description => "将各行中符合指定运行结果的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get; set; } = new ControlItem[]
        {
            new ControlItem("已取消", HashResult.Canceled),
            new ControlItem("已失败", HashResult.Failed),
            new ControlItem("已成功", HashResult.Succeeded),
            //new ControlItem("无结果", HashResult.NoResult),
        };

        public override void SetFilterTags(HashViewModel model)
        {
            if (model != null)
            {
                IEnumerable<HashResult> expResults =
                    this.Items.Cast<ControlItem>().Where(i => i.Selected).Select(i => (HashResult)i.ItemValue);
                if (expResults.Any() && !expResults.Contains(model.Result))
                {
                    model.Matched = false;
                }
            }
        }
    }
}
