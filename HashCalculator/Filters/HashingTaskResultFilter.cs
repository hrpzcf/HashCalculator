using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class HashingTaskResultFilter : AbsHashViewFilter
    {
        private readonly GenericItemModel[] expResultModels = new GenericItemModel[]
        {
            new GenericItemModel("已取消", HashResult.Canceled),
            new GenericItemModel("已失败", HashResult.Failed),
            new GenericItemModel("已成功", HashResult.Succeeded),
        };

        public override ContentControl UserInterface { get; }

        public override string Display => "运行结果";

        public override string Description => "将各行中符合指定运行结果的行筛选出来。";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get => this.expResultModels; set { } }

        public HashingTaskResultFilter()
        {
            this.UserInterface = new HashingTaskResultFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null)
            {
                return;
            }
            HashSet<HashResult> expectedResults = this.expResultModels.Where(i => i.Selected).Select(i => (HashResult)i.ItemValue).ToHashSet();
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
