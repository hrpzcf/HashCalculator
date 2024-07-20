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
        private static readonly PropertyGroupDescription groupIdDesc =
            new PropertyGroupDescription(nameof(HashViewModel.GroupId));
        private static readonly PropertyGroupDescription ehGroupIdDesc =
            new PropertyGroupDescription(nameof(HashViewModel.EhGroupId));

        private AlgoInOutModel[] _algos;
        private bool checkEmbeddedHashValue = false;
        private bool automaticallyFocusAlgorithm = true;

        public override ContentControl UserInterface { get; }

        public override string Display => "相同哈希值";

        public override string Description => "筛选出各行中含有相同哈希值的行并给它们的哈希值列打上相同的颜色标记。";

        public override object Param { get; set; }

        public override object[] Items
        {
            get => this._algos;
            set => this._algos = value as AlgoInOutModel[];
        }

        public override GroupDescription[] GroupDescriptions { get; } =
            new GroupDescription[1];

        public bool CheckEmbeddedHashValue
        {
            get => this.checkEmbeddedHashValue;
            set => this.SetPropNotify(ref this.checkEmbeddedHashValue, value);
        }

        public bool AutomaticallyFocusAlgorithm
        {
            get => this.automaticallyFocusAlgorithm;
            set => this.SetPropNotify(ref this.automaticallyFocusAlgorithm, value);
        }

        public EqualHashByteFilter()
        {
            this._algos = AlgosPanelModel.ProvidedAlgos.Select(
                i => i.NewAlgoInOutModel()).ToArray();
            this.Param = this._algos[0];
            this.UserInterface = new EqualHashByteFilterCtrl(this);
        }

        public override void Reset()
        {
            Settings.Current.ShowHashInTagColumn = false;
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (!this.CheckEmbeddedHashValue)
            {
                this.FilterHashValue(models);
            }
            else
            {
                this.FilterEmbeddedHashValue(models);
            }
        }

        private void FilterHashValue(IEnumerable<HashViewModel> models)
        {
            if (models != null)
            {
                AlgoType focusedAlgoType = default(AlgoType);
                if (this.AutomaticallyFocusAlgorithm)
                {
                    HashViewModel model = models.FirstOrDefault(
                        i => i.CurrentInOutModel != null &&
                        i.CurrentInOutModel.AlgoType != AlgoType.Unknown);
                    if (model != null)
                    {
                        focusedAlgoType = model.CurrentInOutModel.AlgoType;
                    }
                }
                else if (this.Param is AlgoInOutModel)
                {
                    focusedAlgoType = (this.Param as AlgoInOutModel).AlgoType;
                }
                this.GroupDescriptions[0] = groupIdDesc;
                Dictionary<byte[], ModelCurAlgoDict> groupByHashBytes =
                    new Dictionary<byte[], ModelCurAlgoDict>(BytesComparer.Default);
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
                            if (algo.AlgoType == focusedAlgoType)
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
                    new Dictionary<byte[], ModelCurAlgoDict>(BytesComparer.Default);
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
                IEnumerable<ComparableColor> colors = CommonUtils.ColorGenerator(
                    finalHashModels.Count,
                    Settings.Current.LuminanceOfTableCellsWithSameHash,
                    Settings.Current.SaturationOfTableCellsWithSameHash).Select(i => new ComparableColor(i));
                foreach (var tuple in finalHashModels.ZipElements(colors))
                {
                    foreach (HashViewModel model in tuple.Item1.Value.Keys)
                    {
                        model.GroupId = tuple.Item2;
                    }
                }
            }
        }

        private void FilterEmbeddedHashValue(IEnumerable<HashViewModel> models)
        {
            if (models != null)
            {
                this.GroupDescriptions[0] = ehGroupIdDesc;
                Dictionary<byte[], List<HashViewModel>> groupByHashBytes =
                    new Dictionary<byte[], List<HashViewModel>>(BytesComparer.Default);
                foreach (HashViewModel model in models)
                {
                    if (!model.Matched)
                    {
                        continue;
                    }
                    if (!model.ReadAndPopulateHcmData() || model.HcmDataFromFile.Hash == null)
                    {
                        model.Matched = false;
                        continue;
                    }
                    if (groupByHashBytes.ContainsKey(model.HcmDataFromFile.Hash))
                    {
                        groupByHashBytes[model.HcmDataFromFile.Hash].Add(model);
                    }
                    else
                    {
                        groupByHashBytes[model.HcmDataFromFile.Hash] = new List<HashViewModel>()
                        {
                            model
                        };
                    }
                }
                Dictionary<byte[], List<HashViewModel>> finalHashModels =
                    new Dictionary<byte[], List<HashViewModel>>(BytesComparer.Default);
                foreach (KeyValuePair<byte[], List<HashViewModel>> pair in groupByHashBytes)
                {
                    if (pair.Value.Count < 2)
                    {
                        pair.Value[0].Matched = false;
                    }
                    else
                    {
                        finalHashModels.Add(pair.Key, pair.Value);
                    }
                }
                if (!finalHashModels.Any())
                {
                    return;
                }
                IEnumerable<ComparableColor> colors = CommonUtils.ColorGenerator(
                    finalHashModels.Count,
                    Settings.Current.LuminanceOfTableCellsWithSameHash,
                    Settings.Current.SaturationOfTableCellsWithSameHash).Select(i => new ComparableColor(i));
                foreach (var tuple in finalHashModels.ZipElements(colors))
                {
                    foreach (HashViewModel model in tuple.Item1.Value)
                    {
                        model.EhGroupId = tuple.Item2;
                    }
                }
                Settings.Current.ShowHashInTagColumn = true;
            }
        }
    }
}
