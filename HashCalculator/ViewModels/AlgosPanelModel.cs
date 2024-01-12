using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace HashCalculator
{
    internal class AlgosPanelModel : NotifiableModel
    {
        private RelayCommand clearAllSelectedCmd;

        private static readonly AlgoGroupModel _groupOthers = new AlgoGroupModel(
            "其他算法",
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
                // SHA1
                new AlgoInOutModel(new NetCryptoCngSHA1()),
            });
        private static readonly AlgoGroupModel _groupSHA2 = new AlgoGroupModel(
            "SHA2",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new LibRHashSHA224()),
                new AlgoInOutModel(new NetCryptoCngSHA256()),
                new AlgoInOutModel(new NetCryptoCngSHA384()),
                new AlgoInOutModel(new NetCryptoCngSHA512()),
            });
        private static readonly AlgoGroupModel _groupSHA3 = new AlgoGroupModel(
            "SHA3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new ExtendedKcpSHA3(224)),
                new AlgoInOutModel(new ExtendedKcpSHA3(256)),
                new AlgoInOutModel(new ExtendedKcpSHA3(384)),
                new AlgoInOutModel(new ExtendedKcpSHA3(512)),
            });
        private static readonly AlgoGroupModel _groupBLAKE2b = new AlgoGroupModel(
            "BLAKE2B",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2b(224)),
                new AlgoInOutModel(new OfficialImplBlake2b(256)),
                new AlgoInOutModel(new OfficialImplBlake2b(384)),
                new AlgoInOutModel(new OfficialImplBlake2b(512)),
            });
        private static readonly AlgoGroupModel _groupBLAKE2bp = new AlgoGroupModel(
            "BLAKE2BP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2bp(224)),
                new AlgoInOutModel(new OfficialImplBlake2bp(256)),
                new AlgoInOutModel(new OfficialImplBlake2bp(384)),
                new AlgoInOutModel(new OfficialImplBlake2bp(512)),
            });
        private static readonly AlgoGroupModel _groupBLAKE2s = new AlgoGroupModel(
            "BLAKE2S",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2s(224)),
                new AlgoInOutModel(new OfficialImplBlake2s(256)),
            });
        private static readonly AlgoGroupModel _groupBLAKE2sp = new AlgoGroupModel(
            "BLAKE2SP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2sp(224)),
                new AlgoInOutModel(new OfficialImplBlake2sp(256)),
            });
        private static readonly AlgoGroupModel _groupBLAKE3 = new AlgoGroupModel(
            "BLAKE3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake3(224)),
                new AlgoInOutModel(new OfficialImplBlake3(256)),
                new AlgoInOutModel(new OfficialImplBlake3(384)),
                new AlgoInOutModel(new OfficialImplBlake3(512)),
            });
        private static readonly AlgoGroupModel _groupStreebog = new AlgoGroupModel(
            "STREEBOG",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new Gost34112012Streebog(256)),
                new AlgoInOutModel(new Gost34112012Streebog(512)),
            });
        private readonly static AlgoGroupModel _groupAllAlgos = new AlgoGroupModel(
            "总览视图",
            _groupOthers.ConcatItems(
                _groupSHA2, _groupSHA3, _groupBLAKE2b,
                _groupBLAKE2bp, _groupBLAKE2s, _groupBLAKE2sp,
                _groupBLAKE3, _groupStreebog
            ).ToArray());
        private AlgoGroupModel _selectedAlgoGroup = _groupOthers;

        public static AlgoGroupModel[] AlgoGroups { get; } = new AlgoGroupModel[]
            {
                _groupOthers,
                _groupSHA2,
                _groupSHA3,
                _groupBLAKE2b,
                _groupBLAKE2bp,
                _groupBLAKE2s,
                _groupBLAKE2sp,
                _groupBLAKE3,
                _groupStreebog,
                _groupAllAlgos,
            };

        public static AlgoInOutModel[] ProvidedAlgos { get => _groupAllAlgos.Items; }

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
                return this._selectedAlgoGroup;
            }
            set
            {
                this.SetPropNotify(ref this._selectedAlgoGroup, value);
            }
        }

        private void ClearAllSelectedAction(object param)
        {
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
