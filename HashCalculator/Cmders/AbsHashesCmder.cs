using System.Collections.Generic;
using System.Windows.Controls;

namespace HashCalculator
{
    internal abstract class AbsHashesCmder
    {
        protected object RefModels { get; }

        public abstract string Display { get; }

        public abstract string Description { get; }

        public abstract ContentControl UserInterface { get; }

        public virtual void Init() { }

        public virtual void Reset() { }

        public AbsHashesCmder(IEnumerable<HashViewModel> models)
        {
            this.RefModels = models;
        }
    }
}
