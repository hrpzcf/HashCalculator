using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class CmpResultFilter : HashViewFilter<HashViewModel>
    {
        public override string Display => "校验结果";

        public override string Description => "将各行中含有指定校验结果的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get; set; } =
            new ControlItem[] {
            new ControlItem("未校验", CmpRes.NoResult),
            new ControlItem("无关联", CmpRes.Unrelated),
            new ControlItem("已匹配", CmpRes.Matched),
            new ControlItem("不匹配", CmpRes.Mismatch),
            new ControlItem("不确定", CmpRes.Uncertain),
        };

        public override void SetFilterTags(HashViewModel model)
        {
            if (model != null && this.Param is FilterLogic filterLogic)
            {
                IEnumerable<ControlItem> cmpResultCtrls = 
                    this.Items.Cast<ControlItem>().Where(i => i.Selected);
                if (cmpResultCtrls.Any())
                {
                    if (model.AlgoInOutModels == null)
                    {
                        model.Matched = false;
                    }
                    else
                    {
                        HashSet<CmpRes> expResults = cmpResultCtrls.Select(i => (CmpRes)i.ItemValue).ToHashSet();
                        HashSet<CmpRes> modelResults = model.AlgoInOutModels.Select(i => i.HashCmpResult).ToHashSet();
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelResults.Overlaps(expResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelResults.SetEquals(expResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelResults.IsSubsetOf(expResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelResults.IsSupersetOf(expResults))
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
