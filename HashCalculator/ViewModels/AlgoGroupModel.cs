using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HashCalculator
{
    internal class AlgoGroupModel : NotifiableModel
    {
        private int _selectedAlgoCount = 0;

        public AlgoGroupModel(string name, AlgoInOutModel[] models)
        {
            this.GroupName = name;
            this.Items = models ?? throw new ArgumentNullException("models can not be null");
            foreach (AlgoInOutModel model in models)
            {
                model.PropertyChanged += this.ItemSelectionChanged;
            }
        }

        public string GroupName { get; }

        public AlgoInOutModel[] Items { get; }

        public int SelectedAlgoCount
        {
            get => this._selectedAlgoCount;
            set => this.SetPropNotify(ref this._selectedAlgoCount, value);
        }

        public IEnumerable<AlgoInOutModel> CombineItems(params AlgoGroupModel[] groups)
        {
            foreach (AlgoInOutModel model in this.Items)
            {
                yield return model;
            }
            foreach (AlgoGroupModel group in groups)
            {
                foreach (AlgoInOutModel model in group.Items)
                {
                    yield return model;
                }
            }
        }

        private void ItemSelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlgoInOutModel.Selected) && sender is AlgoInOutModel model)
            {
                this.SelectedAlgoCount += model.Selected ? 1 : -1;
            }
        }
    }
}
