using System;

namespace HashCalculator
{
    internal class RegNode
    {
        public RegNode(string name)
        {
            this.Name = name ??
                throw new ArgumentNullException($"Argument can not be null: {nameof(name)}");
        }

        public string Name { get; }

        public RegNode[] Nodes { get; set; }

        public RegValue[] Values { get; set; }
    }
}
