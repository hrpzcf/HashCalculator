using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class CmpResultFilter : AbsHashViewFilter
    {
        private readonly GenericItemModel[] expResultModels = new GenericItemModel[]
        {
            new GenericItemModel("未校验", CmpRes.NoResult),
            new GenericItemModel("无关联", CmpRes.Unrelated),
            new GenericItemModel("已匹配", CmpRes.Matched),
            new GenericItemModel("不匹配", CmpRes.Mismatch),
            new GenericItemModel("不确定", CmpRes.Uncertain),
        };

        public override ContentControl Settings { get; }

        public override string Display => "校验结果";

        public override string Description => "将各行中含有指定校验结果的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items { get => this.expResultModels; set { } }

        public CmpResultFilter()
        {
            this.Settings = new CmpResultFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null || !(this.Param is FilterLogic filterLogic))
            {
                return;
            }
            HashSet<CmpRes> expectedResults = this.expResultModels.Where(i => i.Selected)
                .Select(i => (CmpRes)i.ItemValue).ToHashSet();
            if (expectedResults.Any())
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
                        HashSet<CmpRes> modelResults = model.AlgoInOutModels.Select(i => i.HashCmpResult).ToHashSet();
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelResults.Overlaps(expectedResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelResults.SetEquals(expectedResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelResults.IsSubsetOf(expectedResults))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelResults.IsSupersetOf(expectedResults))
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
