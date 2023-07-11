using System;
using System.IO;
using System.Text;

namespace HashCalculator
{
    internal class MappedWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly long _prevPosition;

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

        public bool WriteProcessId(int processId)
        {
            try
            {
                byte[] bytes = BitConverter.GetBytes(processId);
                if (bytes.Length != MappedBytes.ProcessId)
                {
                    return false;
                }
                this._stream.Position = MappedStart.ProcessId;
                this._stream.Write(bytes, 0, MappedBytes.ProcessId);
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

        public bool WriteRunMulti(bool runMulti)
        {
            try
            {
                byte[] bytes = BitConverter.GetBytes(runMulti);
                if (bytes.Length != MappedBytes.RunMulti)
                {
                    return false;
                }
                this._stream.Position = MappedStart.RunMulti;
                this._stream.Write(bytes, 0, MappedBytes.RunMulti);
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

        public bool WriteLines(string[] lines)
        {
            try
            {
                byte[] vBytes = BitConverter.GetBytes((int)MappedVer.Version1);
                if (vBytes.Length != MappedBytes.Version)
                {
                    return false;
                }
                this._stream.Position = MappedStart.Version;
                this._stream.Write(vBytes, 0, vBytes.Length);
                byte[] itemCountBytes = BitConverter.GetBytes(lines.Length);
                if (itemCountBytes.Length != MappedBytes.ItemCount)
                {
                    return false;
                }
                this._stream.Position = MappedStart.ItemCount;
                this._stream.Write(itemCountBytes, 0, MappedBytes.ItemCount);
                this._stream.Position = MappedStart.FirstItem;
                foreach (string item in lines)
                {
                    byte[] stringItemBytes = Encoding.Unicode.GetBytes(item);
                    byte[] itemLengthBytes = BitConverter.GetBytes(stringItemBytes.Length);
                    if (itemLengthBytes.Length != MappedBytes.ItemLength)
                    {
                        return false;
                    }
                    this._stream.Write(itemLengthBytes, 0, MappedBytes.ItemLength);
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
