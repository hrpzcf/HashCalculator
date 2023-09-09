using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class ModelCurAlgoDict
    {
        public ModelCurAlgoDict()
        {
            this.Dict = new Dictionary<HashViewModel, AlgoInOutModel>();
        }

        public ModelCurAlgoDict(HashViewModel model, AlgoInOutModel algo)
        {
            this.Dict = new Dictionary<HashViewModel, AlgoInOutModel>() { { model, algo } };
        }

        public Dictionary<HashViewModel, AlgoInOutModel> Dict { get; }
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
                Dictionary<byte[], ModelCurAlgoDict> groupByHash =
                    new Dictionary<byte[], ModelCurAlgoDict>(this.comparer);
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
                                            groupByHash[algo.HashResult].Dict.Add(item, algo);
                                        }
                                        else
                                        {
                                            groupByHash[algo.HashResult] = new ModelCurAlgoDict(item, algo);
                                        }
                                        itemMatched = true;
                                    }
                                    break;
                                }
                            }
                            item.Matched = itemMatched;
                        }
                    }
                }
                Dictionary<byte[], ModelCurAlgoDict> equalByteArrayModels =
                    new Dictionary<byte[], ModelCurAlgoDict>(this.comparer);
                this.Result = equalByteArrayModels;
                foreach (KeyValuePair<byte[], ModelCurAlgoDict> pair in groupByHash)
                {
                    if (pair.Value.Dict.Count < 2)
                    {
                        foreach (HashViewModel model in pair.Value.Dict.Keys)
                        {
                            model.Matched = false;
                        }
                    }
                    else
                    {
                        if (!this.CheckFileIndex)
                        {
                            equalByteArrayModels.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            foreach (HashViewModel model in pair.Value.Dict.Keys)
                            {
                                model.FileIndex = model.ModelArg.FilePath.GetFileIndex();
                                model.Matched = model.FileIndex != null;
                            }
                            IGrouping<CmpableFileIndex, HashViewModel>[] groupByFileIndex = pair.Value.Dict.Keys.Where(
                                i => i.FileIndex != null).GroupBy(x => x.FileIndex).ToArray();
                            if (groupByFileIndex.Length > 1)
                            {
                                ModelCurAlgoDict modelCurAlgoDict = new ModelCurAlgoDict();
                                foreach (IGrouping<CmpableFileIndex, HashViewModel> group in groupByFileIndex)
                                {
                                    HashViewModel model = group.FirstOrDefault();
                                    if (model != default(HashViewModel))
                                    {
                                        modelCurAlgoDict.Dict[model] = pair.Value.Dict[model];
                                        foreach (HashViewModel model1 in group.Skip(1))
                                        {
                                            model1.Matched = false;
                                        }
                                    }
                                }
                                equalByteArrayModels.Add(pair.Key, modelCurAlgoDict);
                            }
                            else if (groupByFileIndex.Length > 0)
                            {
                                foreach (HashViewModel model in groupByFileIndex[0])
                                {
                                    model.Matched = false;
                                }
                            }
                        }
                        foreach (KeyValuePair<HashViewModel, AlgoInOutModel> kv in pair.Value.Dict)
                        {
                            kv.Key.CurrentInOutModel = kv.Value;
                        }
                    }
                }
                if (!equalByteArrayModels.Any())
                {
                    return;
                }
                foreach (var tuple in CommonUtils.ColorGenerator(equalByteArrayModels.Count).ZipElements(equalByteArrayModels))
                {
                    foreach (HashViewModel model in tuple.Item2.Value.Dict.Keys)
                    {
                        model.GroupId = new ComparableColor(tuple.Item1);
                    }
                }
            }
        }
    }
}
