using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace HashCalculator
{
    internal class AlgoAndHashModels
    {
        public AlgoAndHashModels()
        {
            this.Items = new Dictionary<HashViewModel, AlgoInOutModel>();
        }

        public AlgoAndHashModels(HashViewModel model, AlgoInOutModel algo)
        {
            this.Items = new Dictionary<HashViewModel, AlgoInOutModel>() { { model, algo } };
        }

        public Dictionary<HashViewModel, AlgoInOutModel> Items { get; }
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

        public bool CheckFileIndex { get; set; } = true;

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
                                            groupByHash[algo.HashResult].Items.Add(item, algo);
                                        }
                                        else
                                        {
                                            groupByHash[algo.HashResult] = new AlgoAndHashModels(item, algo);
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
                Dictionary<byte[], AlgoAndHashModels> equalHashBytes =
                    new Dictionary<byte[], AlgoAndHashModels>(this.comparer);
                this.Result = equalHashBytes;
                foreach (KeyValuePair<byte[], AlgoAndHashModels> pair in groupByHash)
                {
                    if (pair.Value.Items.Count < 2)
                    {
                        foreach (HashViewModel model in pair.Value.Items.Keys)
                        {
                            model.Matched = false;
                        }
                    }
                    else
                    {
                        if (!this.CheckFileIndex)
                        {
                            equalHashBytes.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            foreach (HashViewModel model in pair.Value.Items.Keys)
                            {
                                model.FileIndex = model.ModelArg.FilePath.GetFileIndex();
                            }
                            IGrouping<CmpableFileIndex, HashViewModel>[] byFileIndex = pair.Value.Items.Keys.GroupBy(x => x.FileIndex).ToArray();
                            if (byFileIndex.Length > 1)
                            {
                                AlgoAndHashModels algoAndHashModels = new AlgoAndHashModels();
                                foreach (IGrouping<CmpableFileIndex, HashViewModel> group in byFileIndex)
                                {
                                    HashViewModel model = group.FirstOrDefault();
                                    if (model != default(HashViewModel))
                                    {
                                        algoAndHashModels.Items[model] = pair.Value.Items[model];
                                        foreach (HashViewModel model1 in group.Skip(1))
                                        {
                                            model1.Matched = false;
                                        }
                                    }
                                }
                                equalHashBytes.Add(pair.Key, algoAndHashModels);
                            }
                            else if (byFileIndex.Length > 0)
                            {
                                foreach (HashViewModel model in byFileIndex[0])
                                {
                                    model.Matched = false;
                                }
                            }
                        }
                        foreach (KeyValuePair<HashViewModel, AlgoInOutModel> kv in pair.Value.Items)
                        {
                            kv.Key.CurrentInOutModel = kv.Value;
                        }
                    }
                }
                if (!equalHashBytes.Any())
                {
                    return;
                }
                foreach (var tuple in CommonUtils.ColorGenerator(equalHashBytes.Count).ZipElements(equalHashBytes))
                {
                    foreach (HashViewModel model in tuple.Item2.Value.Items.Keys)
                    {
                        model.GroupId = new ComparableColor(tuple.Item1);
                    }
                }
            }
        }
    }
}
