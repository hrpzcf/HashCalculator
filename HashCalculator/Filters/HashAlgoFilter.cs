using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class HashAlgoFilter : AbsHashViewFilter
    {
        private AlgoInOutModel[] _algos;

        public override ContentControl UserInterface { get; }

        public override string Display => "哈希算法";

        public override string Description => "将各行中含有指定算法的行筛选出来。";

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

        public HashAlgoFilter()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos.Select(
                i => i.NewAlgoInOutModel()).ToArray();
            this.UserInterface = new HashAlgoFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null || !(this.Param is FilterLogic filterLogic))
            {
                return;
            }
            HashSet<AlgoType> expectedAlgos = this._algos.Where(i => i.Selected)
                .Select(i => i.AlgoType).ToHashSet();
            if (expectedAlgos.Any())
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
                        HashSet<AlgoType> modelAlgos = model.AlgoInOutModels.Select(i => i.AlgoType).ToHashSet();
                        if (filterLogic == FilterLogic.Any)
                        {
                            if (!modelAlgos.Overlaps(expectedAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Strict)
                        {
                            if (!modelAlgos.SetEquals(expectedAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Within)
                        {
                            if (!modelAlgos.IsSubsetOf(expectedAlgos))
                            {
                                model.Matched = false;
                            }
                        }
                        else if (filterLogic == FilterLogic.Cover)
                        {
                            if (!modelAlgos.IsSupersetOf(expectedAlgos))
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
