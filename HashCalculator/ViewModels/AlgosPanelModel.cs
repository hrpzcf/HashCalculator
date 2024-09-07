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
                new AlgoInOutModel(new FastXxHashXXH32(), "XXH-32"),
                new AlgoInOutModel(new FastXxHashXXH64(), "XXH-64"),
                new AlgoInOutModel(new FastXxHashXXH3_64(), "XXH364"),
                new AlgoInOutModel(new FastXxHashXXH3_128(), "XXH3128"),
                // SM3
                new AlgoInOutModel(new GmSslSM3(), null),
                // MD4/MD5
                new AlgoInOutModel(new LibRHashMD4(), null),
                new AlgoInOutModel(new NetCryptoCngMD5(), null),
                // CrcHash
                new AlgoInOutModel(new StbrummeRepoCrc32(), null),
                new AlgoInOutModel(new OpenHashTabCrc64(), null),
                // QuickXor
                new AlgoInOutModel(new NamazsoQuickXor(), "QuickXorHash"),
                // Whirlpool
                new AlgoInOutModel(new LibRHashWhirlpool(), null),
                // eD2k
                new AlgoInOutModel(new LibRHashED2K(), null),
                // Has160
                new AlgoInOutModel(new LibRHashHas160(), "Has-160"),
                // RipeMD160
                new AlgoInOutModel(new LibRHashRipeMD160(), "RipeMD-160"),
                // SHA1
                new AlgoInOutModel(new NetCryptoCngSHA1(), "SHA1"),
            });
        private static readonly AlgoGroupModel _groupSHA2 = new AlgoGroupModel(
            "SHA2",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new LibRHashSHA224(), "SHA224"),
                new AlgoInOutModel(new NetCryptoCngSHA256(), "SHA256"),
                new AlgoInOutModel(new NetCryptoCngSHA384(), "SHA384"),
                new AlgoInOutModel(new NetCryptoCngSHA512(), "SHA512"),
            });
        private static readonly AlgoGroupModel _groupSHA3 = new AlgoGroupModel(
            "SHA3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new ExtendedKcpSHA3(224), "SHA3224"),
                new AlgoInOutModel(new ExtendedKcpSHA3(256), "SHA3256"),
                new AlgoInOutModel(new ExtendedKcpSHA3(384), "SHA3384"),
                new AlgoInOutModel(new ExtendedKcpSHA3(512), "SHA3512"),
            });
        private static readonly AlgoGroupModel _groupBLAKE2b = new AlgoGroupModel(
            "BLAKE2B",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2b(224), "Blake2b224"),
                new AlgoInOutModel(new OfficialImplBlake2b(256), "Blake2b256"),
                new AlgoInOutModel(new OfficialImplBlake2b(384), "Blake2b384"),
                new AlgoInOutModel(new OfficialImplBlake2b(512), "Blake2b,Blake2b512"),
            });
        private static readonly AlgoGroupModel _groupBLAKE2bp = new AlgoGroupModel(
            "BLAKE2BP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2bp(224), "Blake2bp224"),
                new AlgoInOutModel(new OfficialImplBlake2bp(256), "Blake2bp256"),
                new AlgoInOutModel(new OfficialImplBlake2bp(384), "Blake2bp384"),
                new AlgoInOutModel(new OfficialImplBlake2bp(512), "Blake2bp,Blake2bp512"),
            });
        private static readonly AlgoGroupModel _groupBLAKE2s = new AlgoGroupModel(
            "BLAKE2S",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2s(224), "Blake2s224"),
                new AlgoInOutModel(new OfficialImplBlake2s(256), "Blake2s,Blake2s256"),
            });
        private static readonly AlgoGroupModel _groupBLAKE2sp = new AlgoGroupModel(
            "BLAKE2SP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2sp(224), "Blake2sp224"),
                new AlgoInOutModel(new OfficialImplBlake2sp(256), "Blake2sp,Blake2sp256"),
            });
        private static readonly AlgoGroupModel _groupBLAKE3 = new AlgoGroupModel(
            "BLAKE3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake3(224), "Blake3224"),
                new AlgoInOutModel(new OfficialImplBlake3(256), "Blake3,Blake3256"),
                new AlgoInOutModel(new OfficialImplBlake3(384), "Blake3384"),
                new AlgoInOutModel(new OfficialImplBlake3(512), "Blake3512"),
            });
        private static readonly AlgoGroupModel _groupStreebog = new AlgoGroupModel(
            "STREEBOG",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new Gost34112012Streebog(256), "Streebog256,GOST-2012-256,GOST 2012 (256)"),
                new AlgoInOutModel(new Gost34112012Streebog(512), "Streebog512,GOST-2012-512,GOST 2012 (512)"),
            });
        private readonly static AlgoGroupModel _groupAllAlgos = new AlgoGroupModel(
            "总览视图",
            _groupOthers.CombineItems(
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

        public static AlgoInOutModel[] ProvidedAlgos => _groupAllAlgos.Items;

        public static bool TryGetAlgoType(string name, out AlgoType algorithm)
        {
            if (name == string.Empty)
            {
                algorithm = AlgoType.UNKNOWN;
                return true;
            }
            if (Enum.TryParse(name.Replace("-", "_"), true, out algorithm))
            {
                return true;
            }
            foreach (AlgoInOutModel model in ProvidedAlgos)
            {
                if (model.IsMyAliasWord(name, StringComparer.OrdinalIgnoreCase))
                {
                    algorithm = model.AlgoType;
                    return true;
                }
            }
            algorithm = AlgoType.UNKNOWN;
            return false;
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

        public static List<AlgoInOutModel> NewInOutModelsByNames(AlgoType[] algoTypes)
        {
            if (algoTypes != null)
            {
                List<AlgoInOutModel> algoInstances = new List<AlgoInOutModel>();
                foreach (AlgoInOutModel model in ProvidedAlgos)
                {
                    if (algoTypes.Contains(model.AlgoType))
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
                if (checklist.TryGetFileHashChecker(fileName, out HashChecker checker))
                {
                    IEnumerable<AlgoInOutModel> inOutModels;
                    AlgoType[] algoTypes = checker.GetExistingAlgoTypes();
                    if (algoTypes.Length != 0)
                    {
                        inOutModels = NewInOutModelsByNames(algoTypes);
                        if (inOutModels != null)
                        {
                            finalInOutModels.AddRange(inOutModels);
                        }
                    }
                    else
                    {
                        inOutModels = NewInOutModelsByDigestLengths(checker.GetExistingDigestLengths());
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
            get => this._selectedAlgoGroup;
            set => this.SetPropNotify(ref this._selectedAlgoGroup, value);
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
