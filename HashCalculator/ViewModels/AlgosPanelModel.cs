using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace HashCalculator
{
    internal class AlgosPanelModel : NotifiableModel
    {
        private RelayCommand checkBoxChangedCmd;
        private RelayCommand clearAllSelectedCmd;

        private AlgoGroupModel _selectedGroup = GroupOthers;

        public static AlgoGroupModel GroupOthers { get; } = new AlgoGroupModel(
            "OTHERS",
            new AlgoInOutModel[]
            {
                // XxHash
                new AlgoInOutModel(new FastXxHashXXH32()),
                new AlgoInOutModel(new FastXxHashXXH64()),
                new AlgoInOutModel(new FastXxHashXXH3_64()),
                new AlgoInOutModel(new FastXxHashXXH3_128()),
                // SM3
                new AlgoInOutModel(new GmSslSM3()),
                // MD4/MD5
                new AlgoInOutModel(new LibRHashMD4()),
                new AlgoInOutModel(new NetCryptoCngMD5()),
                // CrcHash
                new AlgoInOutModel(new StbrummeRepoCrc32()),
                new AlgoInOutModel(new OpenHashTabCrc64()),
                // QuickXor
                new AlgoInOutModel(new NamazsoQuickXor()),
                // Whirlpool
                new AlgoInOutModel(new LibRHashWhirlpool()),
                // eD2k
                new AlgoInOutModel(new LibRHashED2K()),
                // Has160
                new AlgoInOutModel(new LibRHashHas160()),
                // RipeMD160
                new AlgoInOutModel(new LibRHashRipeMD160()),
            });

        public static AlgoGroupModel GroupSHA2 { get; } = new AlgoGroupModel(
            "SHA1/2",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new NetCryptoCngSHA1()), // SHA1
                new AlgoInOutModel(new LibRHashSHA224()),
                new AlgoInOutModel(new NetCryptoCngSHA256()),
                new AlgoInOutModel(new NetCryptoCngSHA384()),
                new AlgoInOutModel(new NetCryptoCngSHA512()),
            });

        public static AlgoGroupModel GroupSHA3 { get; } = new AlgoGroupModel(
            "SHA3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new ExtendedKcpSHA3(224)),
                new AlgoInOutModel(new ExtendedKcpSHA3(256)),
                new AlgoInOutModel(new ExtendedKcpSHA3(384)),
                new AlgoInOutModel(new ExtendedKcpSHA3(512)),
            });

        public static AlgoGroupModel GroupBlake2b { get; } = new AlgoGroupModel(
            "BLAKE2B",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2b(224)),
                new AlgoInOutModel(new OfficialImplBlake2b(256)),
                new AlgoInOutModel(new OfficialImplBlake2b(384)),
                new AlgoInOutModel(new OfficialImplBlake2b(512)),
            });

        public static AlgoGroupModel GroupBlake2bp { get; } = new AlgoGroupModel(
            "BLAKE2BP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2bp(224)),
                new AlgoInOutModel(new OfficialImplBlake2bp(256)),
                new AlgoInOutModel(new OfficialImplBlake2bp(384)),
                new AlgoInOutModel(new OfficialImplBlake2bp(512)),
            });

        public static AlgoGroupModel GroupBlake2s { get; } = new AlgoGroupModel(
            "BLAKE2S",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2s(224)),
                new AlgoInOutModel(new OfficialImplBlake2s(256)),
            });

        public static AlgoGroupModel GroupBlake2sp { get; } = new AlgoGroupModel(
            "BLAKE2SP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2sp(224)),
                new AlgoInOutModel(new OfficialImplBlake2sp(256)),
            });

        public static AlgoGroupModel GroupBlake3 { get; } = new AlgoGroupModel(
            "BLAKE3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake3(224)),
                new AlgoInOutModel(new OfficialImplBlake3(256)),
                new AlgoInOutModel(new OfficialImplBlake3(384)),
                new AlgoInOutModel(new OfficialImplBlake3(512)),
            });

        public static AlgoGroupModel GroupStreebog { get; } = new AlgoGroupModel(
            "STREEBOG",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new Gost34112012Streebog(256)),
                new AlgoInOutModel(new Gost34112012Streebog(512)),
            });

        public static AlgoGroupModel[] AlgoGroups { get; } =
            new AlgoGroupModel[]
            {
                GroupOthers,
                GroupSHA2,
                GroupSHA3,
                GroupBlake2b,
                GroupBlake2bp,
                GroupBlake2s,
                GroupBlake2sp,
                GroupBlake3,
                GroupStreebog,
            };

        public static AlgoInOutModel[] ProvidedAlgos { get; } = GroupOthers.Items
            .Concat(GroupSHA2.Items)
            .Concat(GroupSHA3.Items)
            .Concat(GroupBlake2b.Items)
            .Concat(GroupBlake2bp.Items)
            .Concat(GroupBlake2s.Items)
            .Concat(GroupBlake2sp.Items)
            .Concat(GroupBlake3.Items)
            .Concat(GroupStreebog.Items).ToArray();

        public static AlgoInOutModel[] FromAlgoName(string name)
        {
            return ProvidedAlgos.Where(
                i => i.AlgoName.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(
                i => i.NewAlgoInOutModel()).ToArray();
        }

        public static AlgoInOutModel[] FromAlgoType(AlgoType algoType)
        {
            return ProvidedAlgos.Where(i => i.AlgoType == algoType).Select(
                i => i.NewAlgoInOutModel()).ToArray();
        }

        public static IEnumerable<AlgoInOutModel> GetSelectedAlgos()
        {
            IEnumerable<AlgoInOutModel> selectedAlgos = ProvidedAlgos.Where(
                i => i.Selected).Select(i => i.NewAlgoInOutModel());
            if (!selectedAlgos.Any())
            {
                return new AlgoInOutModel[] { ProvidedAlgos[0].NewAlgoInOutModel() };
            }
            return selectedAlgos;
        }

        public static AlgoInOutModel[] GetKnownAlgos(IEnumerable<AlgoType> algoTypes)
        {
            if (algoTypes != null)
            {
                IEnumerable<AlgoInOutModel> matchingAlgos = ProvidedAlgos.Where(
                    i => algoTypes.Contains(i.IAlgo.AlgoType)).Select(i => i.NewAlgoInOutModel());
                if (matchingAlgos.Any())
                {
                    return matchingAlgos.ToArray();
                }
            }
            return default(AlgoInOutModel[]);
        }

        public static AlgoInOutModel NewInOutModelByType(AlgoType algoType)
        {
            if (algoType != AlgoType.Unknown)
            {
                foreach (AlgoInOutModel model in ProvidedAlgos)
                {
                    if (model.AlgoType == algoType)
                    {
                        return model.NewAlgoInOutModel();
                    }
                }
            }
            return default(AlgoInOutModel);
        }

        public static List<AlgoInOutModel> NewInOutModelsByNames(string[] algoNames)
        {
            if (algoNames != null)
            {
                List<AlgoInOutModel> algoInstances = new List<AlgoInOutModel>();
                foreach (AlgoInOutModel model in ProvidedAlgos)
                {
                    if (algoNames.Contains(model.AlgoName, StringComparer.OrdinalIgnoreCase))
                    {
                        algoInstances.Add(model.NewAlgoInOutModel());
                    }
                }
                return algoInstances;
            }
            return default(List<AlgoInOutModel>);
        }

        public static IEnumerable<AlgoInOutModel> NewInOutModelsByDigestLengths(int[] lengths)
        {
            if (lengths != null)
            {
                List<AlgoInOutModel> algoInstances;
                switch (Settings.Current.FetchAlgorithmOption)
                {
                    case FetchAlgoOption.SELECTED:
                        return GetSelectedAlgos();
                    case FetchAlgoOption.TATMSHDL:
                        algoInstances = new List<AlgoInOutModel>();
                        foreach (AlgoInOutModel algoInOutModel in ProvidedAlgos)
                        {
                            if (lengths.Contains(algoInOutModel.IAlgo.DigestLength))
                            {
                                algoInstances.Add(algoInOutModel.NewAlgoInOutModel());
                            }
                        }
                        return algoInstances;
                    case FetchAlgoOption.TATSAMSHDL:
                        algoInstances = new List<AlgoInOutModel>();
                        foreach (AlgoInOutModel algoInOutModel in ProvidedAlgos)
                        {
                            if (algoInOutModel.Selected && lengths.Contains(algoInOutModel.IAlgo.DigestLength))
                            {
                                algoInstances.Add(algoInOutModel.NewAlgoInOutModel());
                            }
                        }
                        return algoInstances;
                }
            }
            return default(IEnumerable<AlgoInOutModel>);
        }

        public static AlgoInOutModel[] GetAlgsFromChecklist(HashChecklist checklist, string fileName)
        {
            if (checklist != null)
            {
                List<AlgoInOutModel> finalInOutModels = new List<AlgoInOutModel>();
                if (checklist.TryGetAlgHashMapOfFile(fileName, out AlgHashMap algHashMap))
                {
                    IEnumerable<AlgoInOutModel> inOutModels;
                    string[] algoNames = algHashMap.GetExistingAlgoNames();
                    if (algoNames.Length != 0)
                    {
                        inOutModels = NewInOutModelsByNames(algoNames);
                        if (inOutModels != null)
                        {
                            finalInOutModels.AddRange(inOutModels);
                        }
                    }
                    else
                    {
                        inOutModels = NewInOutModelsByDigestLengths(algHashMap.GetExistingDigestLengths());
                        if (inOutModels != null)
                        {
                            finalInOutModels.AddRange(inOutModels);
                        }
                    }
                }
                return finalInOutModels.ToArray();
            }
            return default(AlgoInOutModel[]);
        }

        public AlgoGroupModel SelectedAlgoGroup
        {
            get
            {
                return this._selectedGroup;
            }
            set
            {
                this.SetPropNotify(ref this._selectedGroup, value);
            }
        }

        private void CheckBoxChangedAction(object param)
        {
            if (param is AlgoInOutModel model)
            {
                Settings.Current.RemoveSelectedAlgosChanged();
                if (!model.Selected)
                {
                    Settings.Current.SelectedAlgos.Remove(model.AlgoType);
                }
                else if (!Settings.Current.SelectedAlgos.Contains(model.AlgoType))
                {
                    Settings.Current.SelectedAlgos.Add(model.AlgoType);
                }
            }
        }

        public ICommand CheckBoxChangedCmd
        {
            get
            {
                if (this.checkBoxChangedCmd == null)
                {
                    this.checkBoxChangedCmd = new RelayCommand(this.CheckBoxChangedAction);
                }
                return this.checkBoxChangedCmd;
            }
        }

        private void ClearAllSelectedAction(object param)
        {
            Settings.Current.SelectedAlgos.Clear();
            foreach (AlgoInOutModel info in ProvidedAlgos)
            {
                info.Selected = false;
            }
        }

        public ICommand ClearAllSelectedCmd
        {
            get
            {
                if (this.clearAllSelectedCmd == null)
                {
                    this.clearAllSelectedCmd = new RelayCommand(this.ClearAllSelectedAction);
                }
                return this.clearAllSelectedCmd;
            }
        }
    }
}
