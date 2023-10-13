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
            new AlgoInOutModel(new MsCryptoCngSHA1()),
            // SHA2
            new AlgoInOutModel(new BCSharpSha224()),
            new AlgoInOutModel(new MsCryptoCngSHA256()),
            new AlgoInOutModel(new MsCryptoCngSHA384()),
            new AlgoInOutModel(new MsCryptoCngSHA512()),
            // SHA3
            new AlgoInOutModel(new BCSharpSha3(224)),
            new AlgoInOutModel(new BCSharpSha3(256)),
            new AlgoInOutModel(new BCSharpSha3(384)),
            new AlgoInOutModel(new BCSharpSha3(512)),
            // Whirlpool
            new AlgoInOutModel(new BCSharpWhirlpool()),
            // MD5
            new AlgoInOutModel(new MsCryptoCngMD5()),
            // Crc32
            new AlgoInOutModel(new ForceCrc32NetCrc32()),
            // XxHash
            new AlgoInOutModel(new LibXxHashXXH32()),
            new AlgoInOutModel(new LibXxHashXXH64()),
            new AlgoInOutModel(new LibXxHashXXH3()),
            new AlgoInOutModel(new LibXxHashXXH128()),
            // Blake2s
            new AlgoInOutModel(new BCSharpBlake2s(128)),
            new AlgoInOutModel(new BCSharpBlake2s(160)),
            new AlgoInOutModel(new BCSharpBlake2s(224)),
            new AlgoInOutModel(new BCSharpBlake2s(256)),
            // Blake2b
            new AlgoInOutModel(new BCSharpBlake2b(128)),
            new AlgoInOutModel(new BCSharpBlake2b(160)),
            new AlgoInOutModel(new BCSharpBlake2b(224)),
            new AlgoInOutModel(new BCSharpBlake2b(256)),
            new AlgoInOutModel(new BCSharpBlake2b(384)),
            new AlgoInOutModel(new BCSharpBlake2b(512)),
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
