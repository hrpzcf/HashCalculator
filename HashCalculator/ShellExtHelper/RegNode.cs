using System;

namespace HashCalculator
{
    internal class RegNode
    {
        public RegNode(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            this.Name = name;
        }

        public string Name { get; }

        public RegNode[] Nodes { get; set; }

        public RegValue[] Values { get; set; }
    }
}
