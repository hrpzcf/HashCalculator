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

        public static AlgoInOutModel[] ProvidedAlgos { get; } =
            new AlgoInOutModel[] {
                // SHA1
                new AlgoInOutModel(new MicrosoftSHA1Cng()),
                // SHA2
                new AlgoInOutModel(new BouncyCastleSha224()),
                new AlgoInOutModel(new MicrosoftSHA256Cng()),
                new AlgoInOutModel(new MicrosoftSHA384Cng()),
                new AlgoInOutModel(new MicrosoftSHA512Cng()),
                // SHA3
                new AlgoInOutModel(new BouncyCastleSha3(224)),
                new AlgoInOutModel(new BouncyCastleSha3(256)),
                new AlgoInOutModel(new BouncyCastleSha3(384)),
                new AlgoInOutModel( new BouncyCastleSha3(512)),
                // Whirlpool
                new AlgoInOutModel(new BouncyCastleWhirlpool()),
                // MD5
                new AlgoInOutModel(new MicrosoftMD5Cng()),
                // Blake2s
                new AlgoInOutModel(new BouncyCastleBlake2s(128)),
                new AlgoInOutModel(new BouncyCastleBlake2s(160)),
                new AlgoInOutModel(new BouncyCastleBlake2s(224)),
                new AlgoInOutModel(new BouncyCastleBlake2s(256)),
                // Blake2b
                new AlgoInOutModel(new BouncyCastleBlake2b(128)),
                new AlgoInOutModel(new BouncyCastleBlake2b(160)),
                new AlgoInOutModel(new BouncyCastleBlake2b(224)),
                new AlgoInOutModel(new BouncyCastleBlake2b(256)),
                new AlgoInOutModel(new BouncyCastleBlake2b(384)),
                new AlgoInOutModel(new BouncyCastleBlake2b(512)),
                // Blake3
                new AlgoInOutModel(new BouncyCastleBlake3(128)),
                new AlgoInOutModel(new BouncyCastleBlake3(160)),
                new AlgoInOutModel(new BouncyCastleBlake3(224)),
                new AlgoInOutModel(new BouncyCastleBlake3(256)),
                new AlgoInOutModel(new BouncyCastleBlake3(384)),
                new AlgoInOutModel(new BouncyCastleBlake3(512)),
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
                    Settings.Current.SelectedAlgos.Remove(model.AlgoName);
                }
                else if (!Settings.Current.SelectedAlgos.Contains(model.AlgoName))
                {
                    Settings.Current.SelectedAlgos.Add(model.AlgoName);
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
