using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HashCalculator.ViewModels.Pages;

public class AlgorithmsModel : BaseViewModel
{
    private RelayCommand clearAllSelectedCmd;

    private static readonly AlgoGroupModel _groupOthers = new AlgoGroupModel(
        "未分类算法",
        new AlgoInOutModel[]
        {
            // SM3
            new AlgoInOutModel(new GmSslSM3(), null),
            // MD4/MD5
            new AlgoInOutModel(new RHashMD4(), null),
            new AlgoInOutModel(new NetCryptoMD5(), null),
            // CrcHash
            new AlgoInOutModel(new StbrummeCrc32(), null),
            new AlgoInOutModel(new OpenHashTabCrc64(), null),
            // QuickXor
            new AlgoInOutModel(new NamazsoQuickXor(), "QuickXorHash"),
            // Whirlpool
            new AlgoInOutModel(new RHashWhirlpool(), null),
            // eD2k
            new AlgoInOutModel(new RHashED2K(), null),
            // Has160
            new AlgoInOutModel(new RHashHas160(), "Has-160"),
            // RipeMD160
            new AlgoInOutModel(new RHashRipeMD160(), "RipeMD-160"),
        });
    private static readonly AlgoGroupModel _groupXXHash = new AlgoGroupModel(
        "XXHASH",
        new AlgoInOutModel[]
        {
            // XxHash
            new AlgoInOutModel(new XxHashXXH_32(), "XXH-32"),
            new AlgoInOutModel(new XxHashXXH_64(), "XXH-64"),
            new AlgoInOutModel(new XxHashXXH3_64(), "XXH364"),
            new AlgoInOutModel(new XxHashXXH3_128(), "XXH3128"),
        });
    private static readonly AlgoGroupModel _groupSHA2 = new AlgoGroupModel(
        "SHA2",
        new AlgoInOutModel[]
        {
            // SHA1
            new AlgoInOutModel(new NetCryptoSHA1(), "SHA1"),
            // SHA2
            new AlgoInOutModel(new RHashSHA224(), "SHA224"),
            new AlgoInOutModel(new NetCryptoSHA256(), "SHA256"),
            new AlgoInOutModel(new NetCryptoSHA384(), "SHA384"),
            new AlgoInOutModel(new NetCryptoSHA512(), "SHA512"),
        });
    private static readonly AlgoGroupModel _groupSHA3 = new AlgoGroupModel(
        "SHA3",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new XkcpSHA3(224), "SHA3224"),
            new AlgoInOutModel(new XkcpSHA3(256), "SHA3256"),
            new AlgoInOutModel(new XkcpSHA3(384), "SHA3384"),
            new AlgoInOutModel(new XkcpSHA3(512), "SHA3512"),
        });
    private static readonly AlgoGroupModel _groupBlake2b = new AlgoGroupModel(
        "BLAKE2B",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new OfficialBlake2b(224), "Blake2b224"),
            new AlgoInOutModel(new OfficialBlake2b(256), "Blake2b256"),
            new AlgoInOutModel(new OfficialBlake2b(384), "Blake2b384"),
            new AlgoInOutModel(new OfficialBlake2b(512), "Blake2b,Blake2b512"),
        });
    private static readonly AlgoGroupModel _groupBlake2bp = new AlgoGroupModel(
        "BLAKE2BP",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new OfficialBlake2bp(224), "Blake2bp224"),
            new AlgoInOutModel(new OfficialBlake2bp(256), "Blake2bp256"),
            new AlgoInOutModel(new OfficialBlake2bp(384), "Blake2bp384"),
            new AlgoInOutModel(new OfficialBlake2bp(512), "Blake2bp,Blake2bp512"),
        });
    private static readonly AlgoGroupModel _groupBlake2s = new AlgoGroupModel(
        "BLAKE2S",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new OfficialBlake2s(224), "Blake2s224"),
            new AlgoInOutModel(new OfficialBlake2s(256), "Blake2s,Blake2s256"),
        });
    private static readonly AlgoGroupModel _groupBlake2sp = new AlgoGroupModel(
        "BLAKE2SP",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new OfficialBlake2sp(224), "Blake2sp224"),
            new AlgoInOutModel(new OfficialBlake2sp(256), "Blake2sp,Blake2sp256"),
        });
    private static readonly AlgoGroupModel _groupBlake3 = new AlgoGroupModel(
        "BLAKE3",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new OfficialBlake3(224), "Blake3224"),
            new AlgoInOutModel(new OfficialBlake3(256), "Blake3,Blake3256"),
            new AlgoInOutModel(new OfficialBlake3(384), "Blake3384"),
            new AlgoInOutModel(new OfficialBlake3(512), "Blake3512"),
        });
    private static readonly AlgoGroupModel _groupStreebog = new AlgoGroupModel(
        "STREEBOG",
        new AlgoInOutModel[]
        {
            new AlgoInOutModel(new Gost34_11_2012(256), "Streebog256,GOST-2012-256,GOST 2012 (256)"),
            new AlgoInOutModel(new Gost34_11_2012(512), "Streebog512,GOST-2012-512,GOST 2012 (512)"),
        });
    private static readonly AlgoGroupModel _groupAll = new AlgoGroupModel(
        "总览视图",
        _groupOthers.CombineItems(
            _groupXXHash,
            _groupSHA2,
            _groupSHA3,
            _groupBlake2b,
            _groupBlake2bp,
            _groupBlake2s,
            _groupBlake2sp,
            _groupBlake3,
            _groupStreebog
        ).ToArray());
    private AlgoGroupModel _selectedAlgoGroup = _groupAll;

    public static AlgoGroupModel[] AlgoGroups { get; } = new AlgoGroupModel[]
        {
            _groupAll,
            _groupOthers,
            _groupXXHash,
            _groupSHA2,
            _groupSHA3,
            _groupBlake2b,
            _groupBlake2bp,
            _groupBlake2s,
            _groupBlake2sp,
            _groupBlake3,
            _groupStreebog,
        };

    public static AlgoInOutModel[] ProvidedAlgos => _groupAll.Items;

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

    public static ObservableCollection<AlgoInOutModel> GetKnownAlgos(List<AlgoType> algoTypes)
    {
        if (algoTypes != null)
        {
            IEnumerable<AlgoInOutModel> matchingAlgos = ProvidedAlgos.Where(
                i => algoTypes.Contains(i.IAlgo.AlgoType)).Select(i => i.NewAlgoInOutModel());
            if (matchingAlgos.Any())
            {
                return new ObservableCollection<AlgoInOutModel>(matchingAlgos);
            }
        }
        return default(ObservableCollection<AlgoInOutModel>);
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

    public static ObservableCollection<AlgoInOutModel> GetAlgsFromChecklist(HashChecklist checklist, string fileName)
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
            return new ObservableCollection<AlgoInOutModel>(finalInOutModels);
        }
        return default(ObservableCollection<AlgoInOutModel>);
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
            this.clearAllSelectedCmd ??= new RelayCommand(this.ClearAllSelectedAction);
            return this.clearAllSelectedCmd;
        }
    }
}
