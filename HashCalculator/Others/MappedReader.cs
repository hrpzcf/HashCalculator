using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HashCalculator
{
    internal class MappedReader : IDisposable
    {
        private readonly long _prevPosition;
        private readonly Stream _stream;
        private const int numofVersionBytes = sizeof(int);
        private const int numofItemCountBytes = sizeof(int);
        private const int numofItemLengthBytes = sizeof(int);
        private const int numofProcFlagBytes = sizeof(bool);

        public MappedReader(Stream stream)
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
                this._stream.Position = numofVersionBytes + numofProcFlagBytes;
                int readed = this._stream.Read(buffer, 0, numofItemCountBytes);
                if (readed != numofItemCountBytes)
                {
                    yield break;
                }
                int itemCount = BitConverter.ToInt32(buffer, 0);
                for (int i = 0; i < itemCount; ++i)
                {
                    readed = this._stream.Read(buffer, 0, numofItemLengthBytes);
                    if (readed != numofItemLengthBytes)
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

        public bool ReadProcFlag()
        {
            try
            {
                byte[] flagBuffer = new byte[numofProcFlagBytes];
                this._stream.Position = numofVersionBytes;
                this._stream.Read(flagBuffer, 0, numofProcFlagBytes);
                return BitConverter.ToBoolean(flagBuffer, 0);
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
            MappedVer ver;
            if ((ver = this.ReadVersion()) == MappedVer.Unknown)
            {
                yield break;
            }
            switch (ver)
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
                if (this._stream.Length < numofVersionBytes)
                {
                    return MappedVer.Unknown;
                }
                byte[] buffer = new byte[numofVersionBytes];
                this._stream.Seek(0, SeekOrigin.Begin);
                if (this._stream.Read(buffer, 0, numofVersionBytes) != numofVersionBytes)
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
