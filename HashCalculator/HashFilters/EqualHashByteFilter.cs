using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

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

    internal class EqualHashByteFilter : HashViewFilter
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

        public override GroupDescription[] GroupDescriptions { get; } =
            // 顺序不能反，分组优先级不一样
            new GroupDescription[] {
                new PropertyGroupDescription(nameof(HashViewModel.GroupId)),
                new PropertyGroupDescription(nameof(HashViewModel.FileIndex)),
            };

        public EqualHashByteFilter()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos.Select(
                i => i.NewAlgoInOutModel()).ToArray();
            this.Param = this._algos[0];
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && this.Param is AlgoInOutModel focusedAlgo)
            {
                Dictionary<byte[], ModelCurAlgoDict> groupByHash =
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
                                    if (groupByHash.Keys.Contains(algo.HashResult, this.comparer))
                                    {
                                        groupByHash[algo.HashResult].Dict.Add(model, algo);
                                    }
                                    else
                                    {
                                        groupByHash[algo.HashResult] = new ModelCurAlgoDict(model, algo);
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
                Dictionary<byte[], ModelCurAlgoDict> equalHashModels =
                    new Dictionary<byte[], ModelCurAlgoDict>(this.comparer);
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
                            equalHashModels.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            foreach (HashViewModel model in pair.Value.Dict.Keys)
                            {
                                model.FileIndex = model.ModelArg.FilePath.GetFileIndex();
                                if (model.FileIndex == null)
                                {
                                    model.Matched = false;
                                }
                            }
                            IGrouping<CmpableFileIndex, HashViewModel>[] groupByFileIndex = pair.Value.Dict.Keys
                                .Where(i => i.FileIndex != null).GroupBy(x => x.FileIndex).ToArray();
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
                                equalHashModels.Add(pair.Key, modelCurAlgoDict);
                            }
                            else if (groupByFileIndex.Length == 1)
                            {
                                foreach (HashViewModel model in groupByFileIndex.First())
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
                if (!equalHashModels.Any())
                {
                    return;
                }
                foreach (var tuple in CommonUtils.ColorGenerator(equalHashModels.Count).ZipElements(equalHashModels))
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
