using System;
using System.Windows.Media;

namespace HashCalculator
{
    internal class ComparableColor : IComparable
    {
        private readonly uint colorNumber;

        public Color Color { get; set; }

        public ComparableColor(Color color)
        {
            this.Color = color;
            this.colorNumber = (((uint)color.A) << 24) | (((uint)color.B) << 16) | (((uint)color.G) << 8) | color.R;
        }

        public override int GetHashCode()
        {
            return this.Color.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ComparableColor other)
            {
                return this.Color.Equals(other.Color);
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is ComparableColor other)
            {
                return this.colorNumber.CompareTo(other.colorNumber);
            }
            return -1;
        }
    }
}
