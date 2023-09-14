using System.Collections.Generic;

namespace HashCalculator
{
    internal abstract class HashViewCmder
    {
        protected object RefModels { get; }

        public abstract string Display { get; }

        public abstract string Description { get; }

        public virtual void Init() { }

        public virtual void Reset() { }

        public HashViewCmder(IEnumerable<HashViewModel> models)
        {
            this.RefModels = models;
        }
    }
}
