using System;
using System.Windows.Media;

namespace HashCalculator
{
    internal class ComparableColor : IComparable
    {
        public Color Color { get; set; }

        public int ColorInt { get; set; }

        public ComparableColor(Color color)
        {
            this.Color = color;
            this.ColorInt = (color.A << 24) | (color.B << 16) | (color.G << 8) & color.R;
        }

        public override int GetHashCode()
        {
            return this.Color.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ComparableColor color)
            {
                return this.Color.Equals(color.Color);
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is ComparableColor color)
            {
                return this.ColorInt.CompareTo(color.ColorInt);
            }
            throw new NotImplementedException();
        }
    }
}
