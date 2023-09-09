using System;

namespace HashCalculator
{
    internal class CmpableFileIndex : IComparable
    {
        private readonly uint FileIndexHigh;
        private readonly uint FileIndexLow;
        private readonly uint VolumeSerialNumber;

        public CmpableFileIndex(BY_HANDLE_FILE_INFORMATION info)
        {
            this.FileIndexHigh = info.nFileIndexHigh;
            this.FileIndexLow = info.nFileIndexLow;
            this.VolumeSerialNumber = info.dwVolumeSerialNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.VolumeSerialNumber, this.FileIndexHigh, this.FileIndexLow);
        }

        public override bool Equals(object obj)
        {
            if (obj is CmpableFileIndex other)
            {
                return this.VolumeSerialNumber.Equals(other.VolumeSerialNumber) &&
                    this.FileIndexHigh.Equals(other.FileIndexHigh) && this.FileIndexLow.Equals(other.FileIndexLow);
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is CmpableFileIndex other)
            {
                return this.VolumeSerialNumber > other.VolumeSerialNumber ? 1 : this.VolumeSerialNumber < other.VolumeSerialNumber ? -1
                    : this.FileIndexHigh > other.FileIndexHigh ? 1 : this.FileIndexHigh < other.FileIndexHigh ? -1
                    : this.FileIndexLow > other.FileIndexLow ? 1 : this.FileIndexLow < other.FileIndexLow ? -1 : 0;
            }
            return -1;
        }
    }
}
