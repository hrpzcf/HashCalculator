using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HashCalculator
{
    internal class AlgoAndHashModels
    {
        public AlgoAndHashModels()
        {
            this.Items = new Dictionary<AlgoInOutModel, HashViewModel>();
        }

        public AlgoAndHashModels(AlgoInOutModel algo, HashViewModel model)
        {
            this.Items = new Dictionary<AlgoInOutModel, HashViewModel>() { { algo, model } };
        }

        public Dictionary<AlgoInOutModel, HashViewModel> Items { get; }
    }

    internal class EqualHashByteFilter : HashViewFilter<IEnumerable<HashViewModel>>
    {
        private AlgoInOutModel[] _algos;
        private readonly HashBytesComparer comparer = new HashBytesComparer();

        public override string Display => "相同哈希值";

        public override string Description => "将各行中含有相同哈希值的行筛选出来";

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

        public EqualHashByteFilter()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos.Select(
                i => i.NewAlgoInOutModel()).ToArray();
            this.Param = this._algos[0];
        }

        public override void Finish()
        {
            if (this.Result is Dictionary<byte[], HashSet<HashViewModel>> result)
            {
                foreach (HashSet<HashViewModel> hashSet in result.Values)
                {
                    foreach (HashViewModel model in hashSet)
                    {
                        model.GroupId = default(ComparableColor);
                    }
                }
            }
            this.Result = default(object);
        }

        public override void SetFilterTags(IEnumerable<HashViewModel> models)
        {
            if (this.Param is AlgoInOutModel algoSelected)
            {
                Dictionary<byte[], AlgoAndHashModels> groupByHash =
                    new Dictionary<byte[], AlgoAndHashModels>(this.comparer);
                foreach (HashViewModel item in models)
                {
                    if (item.Matched)
                    {
                        if (item.Result != HashResult.Succeeded)
                        {
                            item.Matched = false;
                        }
                        else
                        {
                            bool itemMatched = false;
                            foreach (AlgoInOutModel algo in item.AlgoInOutModels)
                            {
                                if (algo.AlgoType == algoSelected.AlgoType)
                                {
                                    if (algo.HashResult != null)
                                    {
                                        if (groupByHash.Keys.Contains(algo.HashResult, this.comparer))
                                        {
                                            groupByHash[algo.HashResult].Items.Add(algo, item);
                                        }
                                        else
                                        {
                                            groupByHash[algo.HashResult] = new AlgoAndHashModels(algo, item);
                                        }
                                        itemMatched = true;
                                    }
                                    break;
                                }
                            }
                            if (!itemMatched)
                            {
                                item.Matched = false;
                            }
                        }
                    }
                }
                Dictionary<byte[], AlgoAndHashModels> finalResult =
                    new Dictionary<byte[], AlgoAndHashModels>(this.comparer);
                this.Result = finalResult;
                foreach (KeyValuePair<byte[], AlgoAndHashModels> pair in groupByHash)
                {
                    if (pair.Value.Items.Count > 1)
                    {
                        finalResult.Add(pair.Key, pair.Value);
                        foreach (KeyValuePair<AlgoInOutModel, HashViewModel> kv in pair.Value.Items)
                        {
                            kv.Value.CurrentInOutModel = kv.Key;
                        }
                    }
                    else
                    {
                        foreach (HashViewModel model in pair.Value.Items.Values)
                        {
                            model.Matched = false;
                        }
                    }
                }
                if (finalResult.Any())
                {
                    byte[][] keys = finalResult.Keys.ToArray();
                    Color[] colors = CommonUtils.RandomColorGenerator(finalResult.Count);
                    for (int i = 0; i < finalResult.Count; ++i)
                    {
                        foreach (HashViewModel model in finalResult[keys[i]].Items.Values)
                        {
                            model.GroupId = new ComparableColor(colors[i]);
                        }
                    }
                }
            }
        }
    }
}
