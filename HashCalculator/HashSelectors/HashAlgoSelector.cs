using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class HashAlgoSelector : HashSelector<HashViewModel>
    {
        private AlgoInOutModel[] _algos;

        public override string Display => "哈希算法";

        public override string Description => "将各行中含有指定算法的行筛选出来";

        public override object Param { get; set; } = FilterLogic.Any;

        public override object[] Items
        {
            get
            {
                return this._algos;
            }
            set
            {
                this._algos = value as AlgoInOutModel[];
            }
        }

        public HashAlgoSelector()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos.Select(
                i => i.NewAlgoInOutModel()).ToArray();
        }

        public override void SetFilterTags(HashViewModel model)
        {
            if (model != null && this.Param is FilterLogic filterLogic)
            {
                IEnumerable<AlgoInOutModel> expModels = this._algos.Where(i => i.Selected);
                if (expModels.Any())
                {
                    if (model.AlgoInOutModels == null)
                    {
                        model.Matched = false;
                    }
                    else
                    {
                        HashSet<AlgoType> expAlgos = expModels.Select(i => i.AlgoType).ToHashSet();
                        HashSet<AlgoType> modelAlgos = model.AlgoInOutModels.Select(i => i.AlgoType).ToHashSet();
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelAlgos.Overlaps(expAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelAlgos.SetEquals(expAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelAlgos.IsSubsetOf(expAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelAlgos.IsSupersetOf(expAlgos))
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
