using System;
using System.IO;

namespace HashCalculator
{
    /// <summary>
    /// 用于检测文件是否是由本程序生成的有哈希标记的文件、生成有哈希标记的文件、从有哈希标记的文件还原出原文件。<br/>
    /// </summary>
    internal class HcmDataHelper
    {
        private readonly Stream _stream;

        public HcmDataHelper(Stream stream)
        {
            this._stream = stream;
        }

        private static bool StreamReadable(Stream stream)
        {
            return stream != null && stream.CanSeek && stream.CanRead;
        }

        private static bool StreamWritable(Stream stream)
        {
            return stream != null && stream.CanSeek && stream.CanWrite;
        }

        private static bool AvailableInOutModel(AlgoInOutModel model)
        {
            return model == null || (model.AlgoType != AlgoType.UNKNOWN &&
                model.HashResult?.Length > 0);
        }

        private static bool SetStreamLength(FileStream fileStream, long position)
        {
            if (position > 0L)
            {
                try
                {
                    fileStream.SetLength(position);
                    return true;
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        private bool UseableExternal(Stream stream)
        {
            return stream != this._stream && StreamWritable(stream);
        }

        private bool InternalStreamCopyTo(Stream stream, long start, long count, DoubleProgressModel progress)
        {
            if (start < 0L || count <= 0L || start + count > this._stream.Length)
            {
                return false;
            }
            byte[] buffer = null;
            try
            {
                this._stream.Position = start;
                CommonUtils.Suggest(ref buffer, count);
                double progressValue = 0L;
                long remainingCount = count;
                int actualReadCount = 0;
                int plannedReadCount = buffer.Length;
                while (remainingCount > 0L)
                {
                    if (plannedReadCount > remainingCount)
                    {
                        plannedReadCount = (int)remainingCount;
                    }
                    if ((actualReadCount = this._stream.Read(buffer, 0, plannedReadCount)) == 0)
                    {
                        break;
                    }
                    stream.Write(buffer, 0, actualReadCount);
                    progressValue += actualReadCount;
                    progress.CurrentValue = progressValue / count;
                    remainingCount -= actualReadCount;
                }
                return remainingCount == 0L;
            }
            catch (Exception)
            {
            }
            finally
            {
                CommonUtils.MakeSureBuffer(ref buffer, 0);
            }
            return false;
        }

        private bool WriteHcmData(Stream stream, AlgoInOutModel model)
        {
            if (stream == null)
            {
                stream = this._stream;
            }
            if (StreamWritable(stream))
            {
                HcmData hcmData = new HcmData(stream.Length, marker: true);
                if (model == null || (hcmData.TrySetNameBytes(model.AlgoName) &&
                    hcmData.TrySetHashBytes(model.HashResult)))
                {
                    hcmData.RefreshRandomBytes();
                    return hcmData.TryWriteDataToStream(stream);
                }
            }
            return false;
        }

        /// <summary>
        /// 从文件中读取哈希标记（HCM 标记）
        /// </summary>
        public bool ReadHcmData(out HcmData hcmData)
        {
            if (StreamReadable(this._stream))
            {
                long previousPosition = -1;
                try
                {
                    previousPosition = this._stream.Position;
                    hcmData = new HcmData(this._stream.Length);
                    if (hcmData.TryReadDataFromStream(this._stream))
                    {
                        return true;
                    }
                }
                catch (Exception) { }
                finally
                {
                    if (previousPosition != -1)
                    {
                        try
                        {
                            this._stream.Position = previousPosition;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            hcmData = default(HcmData);
            return false;
        }

        /// <summary>
        /// 从文件中读取哈希标记（HCM 标记）
        /// </summary>
        public static bool ReadHcmData(Stream stream, out HcmData hcmData)
        {
            return new HcmDataHelper(stream).ReadHcmData(out hcmData);
        }

        /// <summary>
        /// 从带有哈希标记 (HCM 标记) 的文件中还原出无标记的文件。
        /// </summary>
        public bool RestoreMarkedFile()
        {
            return StreamWritable(this._stream) &&
                this.ReadHcmData(out HcmData hcmData) &&
                hcmData.DataReliable &&
                SetStreamLength(this._stream as FileStream, hcmData.Position);
        }

        /// <summary>
        /// 从带有哈希标记 (HCM 标记) 的文件中还原出无标记的文件。
        /// </summary>
        public bool RestoreMarkedFile(Stream newStream, DoubleProgressModel progress)
        {
            return this.UseableExternal(newStream) &&
                this.ReadHcmData(out HcmData hcmData) &&
                hcmData.DataReliable &&
                this.InternalStreamCopyTo(newStream, 0L, hcmData.Position, progress);
        }

        /// <summary>
        /// 从带有哈希标记 (HCM 标记) 的文件中还原出无标记的文件。
        /// </summary>
        public bool RestoreMarkedFile(Stream newStream, HcmData hcmData, DoubleProgressModel progress)
        {
            return hcmData?.DataReliable == true &&
                this.UseableExternal(newStream) &&
                this.InternalStreamCopyTo(newStream, 0L, hcmData.Position, progress);
        }

        /// <summary>
        /// 生成有哈希标记的文件，哈希标记 (HCM 标记) 具体格式如下：<br/>
        /// 分隔符，算法名，哈希值，随机数据，HcmData.MARKER，1 字节分隔符长度，1 字节算法名长度，2 字节哈希长度，1 字节随机数据长度
        /// </summary>
        public bool GenerateMarkedFile(AlgoInOutModel model)
        {
            return AvailableInOutModel(model) && this.WriteHcmData(null, model);
        }

        /// <summary>
        /// 生成有哈希标记的文件，哈希标记 (HCM 标记) 具体格式如下：<br/>
        /// 分隔符，算法名，哈希值，随机数据，HcmData.MARKER，1 字节分隔符长度，1 字节算法名长度，2 字节哈希长度，1 字节随机数据长度
        /// </summary>
        public bool GenerateMarkedFile(Stream newStream, AlgoInOutModel model, DoubleProgressModel progress)
        {
            return AvailableInOutModel(model) &&
                StreamReadable(this._stream) &&
                this.UseableExternal(newStream) &&
                this.InternalStreamCopyTo(newStream, 0L, this._stream.Length, progress) &&
                this.WriteHcmData(newStream, model);
        }
    }
}
