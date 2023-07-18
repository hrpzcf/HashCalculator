using Microsoft.Win32;
using System;

namespace HashCalculator
{
    internal class RegValue
    {
        public RegValue(string name, object data, RegistryValueKind kind)
        {
            this.Name = name ??
                throw new ArgumentNullException($"Argument can not be null: {nameof(name)}");
            this.Data = data;
            this.Kind = kind;
        }

        public string Name { get; }

        public object Data { get; }

        public RegistryValueKind Kind { get; }
    }
}
