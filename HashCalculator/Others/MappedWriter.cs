using System;
using System.IO;
using System.Text;

namespace HashCalculator
{
    internal class MappedWriter : IDisposable
    {
        private readonly long _prevPosition;
        private readonly Stream _stream;
        private const int numofVersionBytes = sizeof(int);
        private const int numofItemCountBytes = sizeof(int);
        private const int numofItemLengthBytes = sizeof(int);
        private const int numofProcFlagBytes = sizeof(bool);

        public MappedWriter(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException($"Arg [{nameof(stream)}] can not be null");
            }
            else if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException($"Stream [{nameof(stream)}] can not write or seek");
            }
            this._stream = stream;
            this._prevPosition = stream.Position;
        }

        public void WriteProcFlag(bool exists)
        {
            try
            {
                byte[] existsBytes = BitConverter.GetBytes(exists);
                this._stream.Position = numofVersionBytes;
                this._stream.Write(existsBytes, 0, existsBytes.Length);
            }
            finally
            {
                this._stream.Position = this._prevPosition;
            }
        }

        public bool WriteLines(string[] lines)
        {
            try
            {
                byte[] verBytes = BitConverter.GetBytes((int)MappedVer.Version1);
                if (verBytes.Length != numofVersionBytes)
                {
                    return false;
                }
                this._stream.Seek(0, SeekOrigin.Begin);
                this._stream.Write(verBytes, 0, verBytes.Length);
                byte[] itemCountBytes = BitConverter.GetBytes(lines.Length);
                if (itemCountBytes.Length != numofItemCountBytes)
                {
                    return false;
                }
                this._stream.Seek(numofProcFlagBytes, SeekOrigin.Current);
                this._stream.Write(itemCountBytes, 0, numofItemCountBytes);
                foreach (string item in lines)
                {
                    byte[] stringItemBytes = Encoding.Unicode.GetBytes(item);
                    byte[] itemLengthBytes = BitConverter.GetBytes(stringItemBytes.Length);
                    if (itemLengthBytes.Length != numofItemLengthBytes)
                    {
                        return false;
                    }
                    this._stream.Write(itemLengthBytes, 0, numofItemLengthBytes);
                    this._stream.Write(stringItemBytes, 0, stringItemBytes.Length);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                this._stream.Position = this._prevPosition;
            }
        }

        public void Dispose()
        {
            this._stream.Position = this._prevPosition;
        }
    }
}
