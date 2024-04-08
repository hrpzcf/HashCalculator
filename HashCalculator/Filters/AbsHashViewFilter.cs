using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace HashCalculator
{
    internal abstract class AbsHashViewFilter : NotifiableModel
    {
        private bool _selected = false;

        public AbsHashViewFilter()
        {
            this.FilterChangedCmd = new RelayCommand(
                obj =>
                {
                    if (!this.Selected) { this.Reset(); }
                });
        }

        public ICommand FilterChangedCmd { get; }

        public bool Selected
        {
            get => this._selected;
            set => this.SetPropNotify(ref this._selected, value);
        }

        public abstract string Display { get; }

        public abstract string Description { get; }

        public abstract ContentControl UserInterface { get; }

        public abstract object Param { get; set; }

        public abstract object[] Items { get; set; }

        public virtual SortDescription[] SortDescriptions { get; }

        public virtual GroupDescription[] GroupDescriptions { get; }

        public virtual GenericItemModel[] FilterLogics { get; set; } = new GenericItemModel[]
        {
            new GenericItemModel("满足任意要求", FilterLogic.Any),
            new GenericItemModel("严格满足要求", FilterLogic.Strict),
            new GenericItemModel("在要求范围内", FilterLogic.Within),
            new GenericItemModel("涵盖所有要求", FilterLogic.Cover),
        };

        public virtual void Reset() { }

        public abstract void FilterObjects(IEnumerable<HashViewModel> models);
    }
}
