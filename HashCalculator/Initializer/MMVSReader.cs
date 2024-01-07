using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HashCalculator
{
    internal class MmvsReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly long _prevPosition;

        public MmvsReader(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException($"Arg [{nameof(stream)}] can not be null");
            }
            else if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException($"Stream [{nameof(stream)}] can not read or seek");
            }
            this._stream = stream;
            this._prevPosition = stream.Position;
        }

        private IEnumerator<string> InternalReadVer1()
        {
            try
            {
                int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];
                this._stream.Position = MappedStart.ItemCount;
                int readed = this._stream.Read(buffer, 0, MappedBytes.ItemCount);
                if (readed != MappedBytes.ItemCount)
                {
                    yield break;
                }
                int itemCount = BitConverter.ToInt32(buffer, 0);
                this._stream.Position = MappedStart.FirstItem;
                for (int i = 0; i < itemCount; ++i)
                {
                    readed = this._stream.Read(buffer, 0, MappedBytes.ItemLength);
                    if (readed != MappedBytes.ItemLength)
                    {
                        yield break;
                    }
                    int itemLength = BitConverter.ToInt32(buffer, 0);
                    if (itemLength <= 0)
                    {
                        yield break;
                    }
                    if (bufferSize < itemLength)
                    {
                        bufferSize = itemLength;
                        buffer = new byte[bufferSize];
                    }
                    readed = this._stream.Read(buffer, 0, itemLength);
                    if (readed != itemLength)
                    {
                        yield break;
                    }
                    yield return Encoding.Unicode.GetString(buffer, 0, itemLength);
                }
            }
            finally
            {
                this._stream.Position = this._prevPosition;
            }
        }

        public int ReadProcessId()
        {
            try
            {
                byte[] buffer = new byte[MappedBytes.ProcessId];
                this._stream.Position = MappedStart.ProcessId;
                this._stream.Read(buffer, 0, MappedBytes.ProcessId);
                return BitConverter.ToInt32(buffer, 0);
            }
            catch (Exception)
            {
                return default;
            }
            finally
            {
                this._stream.Position = this._prevPosition;
            }
        }

        public bool ReadRunMulti()
        {
            try
            {
                byte[] buffer = new byte[MappedBytes.RunMulti];
                this._stream.Position = MappedStart.RunMulti;
                this._stream.Read(buffer, 0, MappedBytes.RunMulti);
                return BitConverter.ToBoolean(buffer, 0);
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

        public IEnumerable<string> ReadLines()
        {
            switch (this.ReadVersion())
            {
                default:
                case MappedVer.Unknown:
                    yield break;
                case MappedVer.Version1:
                    IEnumerator<string> enumerator = this.InternalReadVer1();
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                    yield break;
            }
        }

        public MappedVer ReadVersion()
        {
            try
            {
                // 前 4 字节为默认约定的内容排布方案版本
                if (this._stream.Length < MappedBytes.Version)
                {
                    return MappedVer.Unknown;
                }
                byte[] buffer = new byte[MappedBytes.Version];
                this._stream.Position = MappedStart.Version;
                if (this._stream.Read(buffer, 0, MappedBytes.Version) != MappedBytes.Version)
                {
                    return MappedVer.Unknown;
                }
                return (MappedVer)BitConverter.ToInt32(buffer, 0);
            }
            catch (Exception)
            {
                return MappedVer.Unknown;
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
