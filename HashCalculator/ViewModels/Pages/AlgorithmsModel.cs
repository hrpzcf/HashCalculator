using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HashCalculator.ViewModels.Pages;

public class AlgorithmsModel : BaseViewModel
{
    private RelayCommand resetAlgoOrderCmd;
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
    /// <summary>
    /// 所有算法的主列表，是算法排序的唯一依据，同时作为"总览视图"展示的条目集合
    /// </summary>
    private static readonly ObservableCollection<AlgoInOutModel> _providedAlgos =
        new ObservableCollection<AlgoInOutModel>(
            _groupOthers.Items
                .Concat(_groupXXHash.Items)
                .Concat(_groupSHA2.Items)
                .Concat(_groupSHA3.Items)
                .Concat(_groupBlake2b.Items)
                .Concat(_groupBlake2bp.Items)
                .Concat(_groupBlake2s.Items)
                .Concat(_groupBlake2sp.Items)
                .Concat(_groupBlake3.Items)
                .Concat(_groupStreebog.Items));
    private static readonly AlgoGroupModel _groupAll = new AlgoGroupModel(
        "总览视图", _providedAlgos);
    /// <summary>
    /// 内置的默认算法顺序，用于"恢复默认排序"
    /// </summary>
    private static readonly AlgoType[] _defaultAlgoOrder = _providedAlgos.Select(
        i => i.AlgoType).ToArray();
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

    public static ObservableCollection<AlgoInOutModel> ProvidedAlgos => _groupAll.Items;

    /// <summary>
    /// 算法的排序发生变化后触发（拖动排序、恢复默认排序、按配置还原排序）
    /// </summary>
    public static event Action AlgoOrderChanged;

    /// <summary>
    /// 把 source 移动到 target 的前面或后面，并把各分组的条目顺序同步为主列表中的相对顺序。
    /// </summary>
    public static void MoveAlgo(AlgoInOutModel source, AlgoInOutModel target, bool insertAfter)
    {
        if (source == null || target == null || source == target)
        {
            return;
        }
        int sourceIndex = _providedAlgos.IndexOf(source);
        int targetIndex = _providedAlgos.IndexOf(target);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }
        int insertIndex = insertAfter ? targetIndex + 1 : targetIndex;
        // source 被移走后，排在它后面的条目的下标都会减一
        if (sourceIndex < insertIndex)
        {
            insertIndex--;
        }
        if (insertIndex == sourceIndex)
        {
            return;
        }
        _providedAlgos.Move(sourceIndex, insertIndex);
        SyncAllGroupOrders();
        AlgoOrderChanged?.Invoke();
    }

    /// <summary>
    /// 按给定的算法类型顺序重排主列表，order 中没有出现的算法保持默认相对顺序排在末尾。
    /// </summary>
    public static void ApplyAlgoOrder(IEnumerable<AlgoType> order)
    {
        if (order == null)
        {
            return;
        }
        Dictionary<AlgoType, AlgoInOutModel> algosByType = _providedAlgos.ToDictionary(
            i => i.AlgoType);
        List<AlgoInOutModel> expectedOrder = new List<AlgoInOutModel>(_providedAlgos.Count);
        HashSet<AlgoType> arrangedTypes = new HashSet<AlgoType>();
        foreach (AlgoType algoType in order)
        {
            if (arrangedTypes.Add(algoType) &&
                algosByType.TryGetValue(algoType, out AlgoInOutModel arrangedAlgo))
            {
                expectedOrder.Add(arrangedAlgo);
            }
        }
        if (expectedOrder.Count == 0)
        {
            return;
        }
        foreach (AlgoInOutModel algo in _providedAlgos)
        {
            if (!arrangedTypes.Contains(algo.AlgoType))
            {
                expectedOrder.Add(algo);
            }
        }
        // 逐个把目标位置的条目搬到前面，而不是重建集合，因为外部静态绑定了同一个集合实例
        for (int i = 0; i < expectedOrder.Count; i++)
        {
            int currentIndex = _providedAlgos.IndexOf(expectedOrder[i]);
            if (currentIndex > i)
            {
                _providedAlgos.Move(currentIndex, i);
            }
        }
        SyncAllGroupOrders();
        AlgoOrderChanged?.Invoke();
    }

    /// <summary>
    /// 把算法的顺序恢复为内置的默认顺序
    /// </summary>
    public static void ResetAlgoOrder()
    {
        ApplyAlgoOrder(_defaultAlgoOrder);
    }

    private static void SyncAllGroupOrders()
    {
        // 总览视图的条目集合就是主列表，同步它自身不会产生变化
        foreach (AlgoGroupModel group in AlgoGroups)
        {
            group.SyncOrderFrom(_providedAlgos);
        }
    }

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

    public static IEnumerable<AlgoInOutModel> NewInOutModelsByNames(HashSet<AlgoType> algoTypes)
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
        return default(IEnumerable<AlgoInOutModel>);
    }

    public static IEnumerable<AlgoInOutModel> NewInOutModelsByDigestLengths(HashSet<int> lengths)
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
                HashSet<AlgoType> algoTypes = checker.GetExistingAlgoTypes();
                if (algoTypes.Count != 0)
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

    private void ResetAlgoOrderAction(object param)
    {
        ResetAlgoOrder();
    }

    public ICommand ResetAlgoOrderCmd
    {
        get
        {
            this.resetAlgoOrderCmd ??= new RelayCommand(this.ResetAlgoOrderAction);
            return this.resetAlgoOrderCmd;
        }
    }
}
