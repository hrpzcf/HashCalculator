using System;
using System.Collections.Generic;

namespace HashCalculator
{
    internal class StringComparer : IEqualityComparer<string>
    {
        public StringComparer() { }

        public StringComparer(bool ignoreCase)
        {
            this.IgnoreCase = ignoreCase;
        }

        public bool IgnoreCase { get; }

        public bool Equals(string x, string y)
        {
            if (this.IgnoreCase)
            {
                return x.Equals(y, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public int GetHashCode(string str)
        {
            return this.IgnoreCase ? str.ToUpper().GetHashCode() : str.GetHashCode();
        }
    }
}
