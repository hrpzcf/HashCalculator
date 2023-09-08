using System;

namespace HashCalculator
{
    internal class CmpableFileIndex : IComparable
    {
        public uint VolumeSerialNumber { get; set; }

        public uint FileIndexHigh { get; set; }

        public uint FileIndexLow { get; set; }

        public CmpableFileIndex(BY_HANDLE_FILE_INFORMATION info)
        {
            this.VolumeSerialNumber = info.dwVolumeSerialNumber;
            this.FileIndexHigh = info.nFileIndexHigh;
            this.FileIndexLow = info.nFileIndexLow;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.VolumeSerialNumber, this.FileIndexHigh, this.FileIndexLow);
        }

        public override bool Equals(object obj)
        {
            if (obj is CmpableFileIndex fileIndex)
            {
                return this.VolumeSerialNumber.Equals(fileIndex.VolumeSerialNumber) &&
                    this.FileIndexHigh.Equals(fileIndex.FileIndexHigh) && this.FileIndexLow.Equals(fileIndex.FileIndexLow);
            }
            return false;
        }

        public int CompareTo(object obj)
        {
            if (obj is CmpableFileIndex fileIndex)
            {
                ulong index1 = ((ulong)this.VolumeSerialNumber << 32) | (this.FileIndexHigh << 16) | this.FileIndexLow;
                ulong index2 = ((ulong)fileIndex.VolumeSerialNumber << 32) | (fileIndex.FileIndexHigh << 16) | fileIndex.FileIndexLow;
                return index1.CompareTo(index2);
            }
            throw new NotImplementedException();
        }
    }
}
