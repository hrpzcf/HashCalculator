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
                new AlgoInOutModel(new FastXxHashXXH3()),
                new AlgoInOutModel(new FastXxHashXXH128()),
                // MD5
                new AlgoInOutModel(new NetCryptoCngMD5()),
                // CrcHash
                new AlgoInOutModel(new StbrummeRepoCrc32()),
                new AlgoInOutModel(new OpenHashTabCrc64()),
                // QuickXor
                new AlgoInOutModel(new NamazsoQuickXor()),
                // Whirlpool
                new AlgoInOutModel(new HashratWhirlpool()),
            });

        public static AlgoGroupModel GroupSHA { get; } = new AlgoGroupModel(
            "SHA",
            new AlgoInOutModel[]
            {
                // SHA1
                new AlgoInOutModel(new NetCryptoCngSHA1()),
                // SHA2
                new AlgoInOutModel(new HaclSha2Sha224()),
                new AlgoInOutModel(new NetCryptoCngSHA256()),
                new AlgoInOutModel(new NetCryptoCngSHA384()),
                new AlgoInOutModel(new NetCryptoCngSHA512()),
                // SHA3
                new AlgoInOutModel(new ExtendedKcpSha3(224)),
                new AlgoInOutModel(new ExtendedKcpSha3(256)),
                new AlgoInOutModel(new ExtendedKcpSha3(384)),
                new AlgoInOutModel(new ExtendedKcpSha3(512)),
            });

        public static AlgoGroupModel GroupBlake2b { get; } = new AlgoGroupModel(
            "BLAKE2B",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2b(128)),
                new AlgoInOutModel(new OfficialImplBlake2b(160)),
                new AlgoInOutModel(new OfficialImplBlake2b(224)),
                new AlgoInOutModel(new OfficialImplBlake2b(256)),
                new AlgoInOutModel(new OfficialImplBlake2b(384)),
                new AlgoInOutModel(new OfficialImplBlake2b(512)),
            });

        public static AlgoGroupModel GroupBlake2bp { get; } = new AlgoGroupModel(
            "BLAKE2BP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2bp(128)),
                new AlgoInOutModel(new OfficialImplBlake2bp(160)),
                new AlgoInOutModel(new OfficialImplBlake2bp(224)),
                new AlgoInOutModel(new OfficialImplBlake2bp(256)),
                new AlgoInOutModel(new OfficialImplBlake2bp(384)),
                new AlgoInOutModel(new OfficialImplBlake2bp(512)),
            });

        public static AlgoGroupModel GroupBlake2s { get; } = new AlgoGroupModel(
            "BLAKE2S",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2s(128)),
                new AlgoInOutModel(new OfficialImplBlake2s(160)),
                new AlgoInOutModel(new OfficialImplBlake2s(224)),
                new AlgoInOutModel(new OfficialImplBlake2s(256)),
            });

        public static AlgoGroupModel GroupBlake2sp { get; } = new AlgoGroupModel(
            "BLAKE2SP",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake2sp(128)),
                new AlgoInOutModel(new OfficialImplBlake2sp(160)),
                new AlgoInOutModel(new OfficialImplBlake2sp(224)),
                new AlgoInOutModel(new OfficialImplBlake2sp(256)),
            });

        public static AlgoGroupModel GroupBlake3 { get; } = new AlgoGroupModel(
            "BLAKE3",
            new AlgoInOutModel[]
            {
                new AlgoInOutModel(new OfficialImplBlake3(128)),
                new AlgoInOutModel(new OfficialImplBlake3(160)),
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
                GroupSHA,
                GroupBlake2b,
                GroupBlake2bp,
                GroupBlake2s,
                GroupBlake2sp,
                GroupBlake3,
                GroupStreebog,
            };

        public static AlgoInOutModel[] ProvidedAlgos { get; } = GroupOthers.Items
            .Concat(GroupSHA.Items)
            .Concat(GroupBlake2b.Items)
            .Concat(GroupBlake2bp.Items)
            .Concat(GroupBlake2s.Items)
            .Concat(GroupBlake2sp.Items)
            .Concat(GroupBlake3.Items)
            .Concat(GroupStreebog.Items).ToArray();

        public static AlgoInOutModel[] FromAlgoName(string name)
        {
            return ProvidedAlgos.Where(
                i => i.AlgoName.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.NewAlgoInOutModel()).ToArray();
        }

        public static AlgoInOutModel[] FromAlgoType(AlgoType algoType)
        {
            return ProvidedAlgos.Where(i => i.AlgoType == algoType).Select(
                i => i.NewAlgoInOutModel()).ToArray();
        }

        public static AlgoInOutModel[] GetSelectedAlgos()
        {
            IEnumerable<AlgoInOutModel> selectedAlgos = ProvidedAlgos.Where(
                i => i.Selected).Select(i => i.NewAlgoInOutModel());
            if (selectedAlgos.Any())
            {
                return selectedAlgos.ToArray();
            }
            return new AlgoInOutModel[] { ProvidedAlgos[0].NewAlgoInOutModel() };
        }

        public static AlgoInOutModel[] GetKnownAlgos(AlgoType algoType)
        {
            if (algoType != AlgoType.Unknown)
            {
                IEnumerable<AlgoInOutModel> matchedAlgo = ProvidedAlgos.Where(
                    i => i.IAlgo.AlgoType == algoType).Select(i => i.NewAlgoInOutModel());
                if (matchedAlgo.Any())
                {
                    return matchedAlgo.ToArray();
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

        public static AlgoInOutModel NewInOutModelByName(string algoName)
        {
            if (!string.IsNullOrEmpty(algoName))
            {
                foreach (AlgoInOutModel model in ProvidedAlgos)
                {
                    if (model.AlgoName.Equals(algoName, StringComparison.OrdinalIgnoreCase))
                    {
                        return model.NewAlgoInOutModel();
                    }
                }
            }
            return default(AlgoInOutModel);
        }

        public static AlgoInOutModel[] GetAlgosFromBasis(HashBasis basis, string fileName)
        {
            if (basis != null)
            {
                List<AlgoInOutModel> algoInOutModels = new List<AlgoInOutModel>();
                if (basis.FileHashDict.ContainsKey(fileName))
                {
                    foreach (string existing in basis.FileHashDict[fileName].GetExistingAlgoNames())
                    {
                        algoInOutModels.Add(NewInOutModelByName(existing));
                    }
                }
                return algoInOutModels.Where(i => i != null).ToArray();
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
