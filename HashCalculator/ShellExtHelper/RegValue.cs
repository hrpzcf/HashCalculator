using System.Diagnostics;
using Microsoft.Win32;

namespace HashCalculator
{
    internal class RegValue
    {
        public RegValue(string name)
        {
            this.Name = name;
            this.Data = string.Empty;
        }

        public RegValue(string name, RegistryValueKind kind)
        {
            this.Name = name;
            this.Data = string.Empty;
            this.Kind = kind;
        }

        public RegValue(string name, object data)
        {
            this.Name = name;
            Debug.Assert(data != null, $"Argument can not be null: {nameof(data)}");
            this.Data = data;
        }

        public RegValue(string name, object data, RegistryValueKind kind)
        {
            this.Name = name;
            Debug.Assert(data != null, $"Argument can not be null: {nameof(data)}");
            this.Data = data;
            this.Kind = kind;
        }

        public string Name { get; }

        public object Data { get; }

        public RegistryValueKind Kind { get; }
    }
}
