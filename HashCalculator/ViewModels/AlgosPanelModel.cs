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

        public static AlgoInOutModel[] ProvidedAlgos { get; } = new AlgoInOutModel[]
        {
            // SHA1
            new AlgoInOutModel(new NetCryptoCngSHA1()),
            // SHA2
            new AlgoInOutModel(new HaclSha2Sha224()),
            new AlgoInOutModel(new NetCryptoCngSHA256()),
            new AlgoInOutModel(new NetCryptoCngSHA384()),
            new AlgoInOutModel(new NetCryptoCngSHA512()),
            // SHA3
            new AlgoInOutModel(new ExtendedKCPSha3(224)),
            new AlgoInOutModel(new ExtendedKCPSha3(256)),
            new AlgoInOutModel(new ExtendedKCPSha3(384)),
            new AlgoInOutModel(new ExtendedKCPSha3(512)),
            // Streebog
            new AlgoInOutModel(new Gost34112012Streebog(256)),
            new AlgoInOutModel(new Gost34112012Streebog(512)),
            // Whirlpool
            new AlgoInOutModel(new HashratWhirlpool()),
            // MD5
            new AlgoInOutModel(new NetCryptoCngMD5()),
            // Crc32
            new AlgoInOutModel(new ForceCrc32NetCrc32()),
            // XxHash
            new AlgoInOutModel(new ExtremelyFastXXH32()),
            new AlgoInOutModel(new ExtremelyFastXXH64()),
            new AlgoInOutModel(new ExtremelyFastXXH3()),
            new AlgoInOutModel(new ExtremelyFastXXH128()),
            // Blake2s
            new AlgoInOutModel(new OfficialImplBlake2s(128)),
            new AlgoInOutModel(new OfficialImplBlake2s(160)),
            new AlgoInOutModel(new OfficialImplBlake2s(224)),
            new AlgoInOutModel(new OfficialImplBlake2s(256)),
            // Blake2sp
            new AlgoInOutModel(new OfficialImplBlake2sp(128)),
            new AlgoInOutModel(new OfficialImplBlake2sp(160)),
            new AlgoInOutModel(new OfficialImplBlake2sp(224)),
            new AlgoInOutModel(new OfficialImplBlake2sp(256)),
            // Blake2b
            new AlgoInOutModel(new OfficialImplBlake2b(128)),
            new AlgoInOutModel(new OfficialImplBlake2b(160)),
            new AlgoInOutModel(new OfficialImplBlake2b(224)),
            new AlgoInOutModel(new OfficialImplBlake2b(256)),
            new AlgoInOutModel(new OfficialImplBlake2b(384)),
            new AlgoInOutModel(new OfficialImplBlake2b(512)),
            // Blake2bp
            new AlgoInOutModel(new OfficialImplBlake2bp(128)),
            new AlgoInOutModel(new OfficialImplBlake2bp(160)),
            new AlgoInOutModel(new OfficialImplBlake2bp(224)),
            new AlgoInOutModel(new OfficialImplBlake2bp(256)),
            new AlgoInOutModel(new OfficialImplBlake2bp(384)),
            new AlgoInOutModel(new OfficialImplBlake2bp(512)),
            // Blake3
            new AlgoInOutModel(new Blake3NetBlake3(128)),
            new AlgoInOutModel(new Blake3NetBlake3(160)),
            new AlgoInOutModel(new Blake3NetBlake3(224)),
            new AlgoInOutModel(new Blake3NetBlake3(256)),
            new AlgoInOutModel(new Blake3NetBlake3(384)),
            new AlgoInOutModel(new Blake3NetBlake3(512)),
        };

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

        public static AlgoInOutModel[] GetAlgosFromBasis(string fileName, HashBasis basis)
        {
            if (basis != null)
            {
                List<AlgoInOutModel> algoInOutModels = new List<AlgoInOutModel>();
                string matchedFileName = null;
                foreach (string nameInBasis in basis.FileHashDict.Keys)
                {
                    if (nameInBasis.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedFileName = nameInBasis;
                        break;
                    }
                }
                if (matchedFileName != null)
                {
                    foreach (string name in basis
                        .FileHashDict[matchedFileName].GetExistsAlgoNames())
                    {
                        foreach (AlgoInOutModel model in ProvidedAlgos)
                        {
                            if (model.AlgoName.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                algoInOutModels.Add(model.NewAlgoInOutModel());
                                break;
                            }
                        }
                    }
                }
                return algoInOutModels.ToArray();
            }
            return default(AlgoInOutModel[]);
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
