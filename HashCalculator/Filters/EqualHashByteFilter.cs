using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace HashCalculator
{
    /// <summary>
    /// 作为 Dictionary<HashViewModel, AlgoInOutModel> 的别名
    /// </summary>
    internal class ModelCurAlgoDict : Dictionary<HashViewModel, AlgoInOutModel>
    {
        public ModelCurAlgoDict(HashViewModel model, AlgoInOutModel algo) : base()
        {
            this.Add(model, algo);
        }
    }

    internal class EqualHashByteFilter : AbsHashViewFilter
    {
        private AlgoInOutModel[] _algos;
        private readonly HashBytesComparer comparer = new HashBytesComparer();

        public override ContentControl Settings { get; }

        public override string Display => "相同哈希值";

        public override string Description => "筛选出各行中含有相同哈希值的行并给它们的哈希值列打上相同的颜色标记";

        public override object Param { get; set; }

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

        public override GroupDescription[] GroupDescriptions { get; } =
            new GroupDescription[] {
                new PropertyGroupDescription(nameof(HashViewModel.GroupId)),
            };

        public EqualHashByteFilter()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos
                .Select(i => i.NewAlgoInOutModel()).ToArray();
            this.Param = this._algos[0];
            this.Settings = new EqualHashByteFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && this.Param is AlgoInOutModel focusedAlgo)
            {
                Dictionary<byte[], ModelCurAlgoDict> groupByHashBytes =
                    new Dictionary<byte[], ModelCurAlgoDict>(this.comparer);
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
                        bool modelMatched = false;
                        foreach (AlgoInOutModel algo in model.AlgoInOutModels)
                        {
                            if (algo.AlgoType == focusedAlgo.AlgoType)
                            {
                                if (algo.HashResult != null)
                                {
                                    if (groupByHashBytes.ContainsKey(algo.HashResult))
                                    {
                                        groupByHashBytes[algo.HashResult].Add(model, algo);
                                    }
                                    else
                                    {
                                        groupByHashBytes[algo.HashResult] = new ModelCurAlgoDict(model, algo);
                                    }
                                    modelMatched = true;
                                }
                                break;
                            }
                        }
                        if (!modelMatched)
                        {
                            model.Matched = false;
                        }
                    }
                }
                Dictionary<byte[], ModelCurAlgoDict> finalHashModels =
                    new Dictionary<byte[], ModelCurAlgoDict>(this.comparer);
                foreach (KeyValuePair<byte[], ModelCurAlgoDict> pair in groupByHashBytes)
                {
                    if (pair.Value.Count < 2)
                    {
                        foreach (HashViewModel model in pair.Value.Keys)
                        {
                            model.Matched = false;
                        }
                    }
                    else
                    {
                        finalHashModels.Add(pair.Key, pair.Value);
                        foreach (KeyValuePair<HashViewModel, AlgoInOutModel> kv in pair.Value)
                        {
                            kv.Key.CurrentInOutModel = kv.Value;
                        }
                    }
                }
                if (!finalHashModels.Any())
                {
                    return;
                }
                foreach (var tuple in CommonUtils.ColorGenerator(finalHashModels.Count).ZipElements(finalHashModels))
                {
                    foreach (HashViewModel model in tuple.Item2.Value.Keys)
                    {
                        model.GroupId = new ComparableColor(tuple.Item1);
                    }
                }
            }
        }
    }
}
