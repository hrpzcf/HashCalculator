using System;
using System.IO;
using System.Text;

namespace HashCalculator
{
    /// <summary>
    /// 用于检测文件是否是由本程序生成的有哈希标记的文件、生成有哈希标记的文件、从有哈希标记的文件还原出原文件。<br/>
    /// </summary>
    internal class FileDataHelper : IDisposable
    {
        /// <summary>
        /// JPEG 文件数据块中的类型标记/数据域长度占用的字节数
        /// </summary>
        private const int JPEG_COUNT_CHUNK_DATA_BYTES = 2;
        /// <summary>
        /// JPEG 扫描开始标记
        /// </summary>
        private static readonly byte[] JPEG_SOI = { 0xFF, 0xD8 };
        /// <summary>
        /// JPEG 扫描开始标记
        /// </summary>
        private static readonly byte[] JPEG_EOI = { 0xFF, 0xD9 };
        /// <summary>
        /// JPEG 扫描开始标记
        /// </summary>
        private static readonly byte[] JPEG_SOS = { 0xFF, 0xDA };

        /// <summary>
        /// 随机数据块的长度(字节)下限
        /// </summary>
        private const int RANDOM_DATA_LENTGH_LOWER = 2;
        /// <summary>
        /// 随机数据块的长度(字节)上限
        /// </summary>
        private const int RANDOM_DATA_LENTGH_UPPER = 8;
        /// <summary>
        /// 记录算法名长度的数值所占用的字节数
        /// </summary>
        private const int COUNT_BYTES_RECORD_NAME_LENGTH = 1;
        /// <summary>
        /// 记录哈希值长度的数值所占用的字节数
        /// </summary>
        private const int COUNT_BYTES_RECORD_HASH_LENGTH = 2;
        /// <summary>
        /// 记录随机数据长度的数值所占用的字节数
        /// </summary>
        private const int COUNT_BYTES_RECORD_RAND_LENGTH = 1;

        /// <summary>
        /// PNG 文件头部固定字节
        /// </summary>
        private static readonly byte[] PNG_HEAD =
            { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        /// <summary>
        /// PNG 图像结束数据块 IEND 包含的类型码
        /// </summary>
        private static readonly byte[] PNG_IEND_TYPE = { 0x49, 0x45, 0x4E, 0x44 };
        /// <summary>
        /// PNG 图像结束数据块 IEND 包含的校验和
        /// </summary>
        private static readonly byte[] PNG_IEND_SUMS = { 0xAE, 0x42, 0x60, 0x82 };
        /// <summary>
        /// PNG 文件数据块中数据域长度的数值/类型码/校验和占用的字节数
        /// </summary>
        private const int PNG_COUNT_CHUNK_DATA_BYTES = 4;

        /// <summary>
        /// 本程序自定义的 HCM 文件头，用于标记文件被本程序写入过哈希标记。<br/>
        /// 对于 PNG/JPEG 文件来说也可能放在尾部，取决于调用 GenerateTaggedFile 方法时的 tailTag 参数。
        /// </summary>
        private static readonly byte[] HC_HEAD =
            { 0xF3, 0x48, 0x43, 0x4D, 0x20, 0x66, 0x69, 0x6C, 0x65 };

        private Stream InitialStream { get; }

        private long InitialPosition { get; } = -1L;

        public FileDataHelper(Stream stream)
        {
            this.InitialStream = stream;
            try
            {
                this.InitialPosition = stream.Position;
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (this.InitialPosition != -1L)
            {
                try
                {
                    this.InitialStream.Position = this.InitialPosition;
                }
                catch (Exception)
                {
                }
            }
        }

        private bool ValidInitialStream
        {
            get
            {
                return this.InitialStream != null &&
                    this.InitialStream.CanRead && !this.InitialStream.CanWrite && this.InitialStream.CanSeek;
            }
        }

        /// <summary>
        /// 获取 PNG 文件的真实大小(不包含 0x49 45 4E 44 AE 42 60 82 标记后的内容)，
        /// 如果返回 0 则代表不是 PNG 文件。
        /// </summary>
        private long GetPngRealLength()
        {
            byte[] bytesBuffer = null;
            const long endTagNotFound = 0L;
            try
            {
                if (!this.ValidInitialStream)
                {
                    return endTagNotFound;
                }
                if (this.InitialStream.Length <= 20L)
                {
                    return endTagNotFound;
                }
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, PNG_HEAD.Length);
                this.InitialStream.Position = 0L;
                if (this.InitialStream.Read(bytesBuffer, 0, PNG_HEAD.Length) != PNG_HEAD.Length ||
                    !PNG_HEAD.ElementsEqual(bytesBuffer))
                {
                    return endTagNotFound;
                }
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, PNG_COUNT_CHUNK_DATA_BYTES);
                while (true)
                {
                    // 读取数据域长度
                    if (this.InitialStream.Read(bytesBuffer, 0, PNG_COUNT_CHUNK_DATA_BYTES) != PNG_COUNT_CHUNK_DATA_BYTES)
                    {
                        return endTagNotFound;
                    }
                    // PNG 内数据域长度是以大端字节序储存的，需要翻转
                    Array.Reverse(bytesBuffer, 0, PNG_COUNT_CHUNK_DATA_BYTES);
                    int dataBlockLength = BitConverter.ToInt32(bytesBuffer, 0);
                    // 读取数据块类型码
                    if (this.InitialStream.Read(bytesBuffer, 0, PNG_COUNT_CHUNK_DATA_BYTES) != PNG_COUNT_CHUNK_DATA_BYTES)
                    {
                        return endTagNotFound;
                    }
                    if (PNG_IEND_TYPE.ElementsEqual(bytesBuffer))
                    {
                        // 读取 IEND 数据块中紧跟在类型码后的校验和
                        if (this.InitialStream.Read(bytesBuffer, 0, PNG_COUNT_CHUNK_DATA_BYTES) != PNG_COUNT_CHUNK_DATA_BYTES)
                        {
                            return endTagNotFound;
                        }
                        if (PNG_IEND_SUMS.ElementsEqual(bytesBuffer))
                        {
                            return this.InitialStream.Position;
                        }
                        else
                        {
                            // 读取类型码后又读取 4 字节，不用再加校验和长度
                            this.InitialStream.Position += dataBlockLength;
                        }
                    }
                    else
                    {
                        // 数据域长度和校验和长度
                        this.InitialStream.Position += dataBlockLength + PNG_COUNT_CHUNK_DATA_BYTES;
                    }
                }
            }
            catch (Exception)
            {
                return endTagNotFound;
            }
            finally
            {
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, 0);
            }
        }

        /// <summary>
        /// 获取 JPEG 文件的真实大小(不包含 0xFF D9 标记后的内容)，如果返回 0 则代表不是 JPEG 文件。
        /// </summary>
        private long GetJpegRealLength()
        {
            byte[] bytesBuffer = null;
            const long endTagNotFound = 0L;
            try
            {
                if (!this.ValidInitialStream)
                {
                    return endTagNotFound;
                }
                if (this.InitialStream.Length <= 4L)
                {
                    return endTagNotFound;
                }
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, JPEG_SOI.Length);
                this.InitialStream.Position = 0L;
                if (this.InitialStream.Read(bytesBuffer, 0, JPEG_SOI.Length) != JPEG_SOI.Length ||
                    !JPEG_SOI.ElementsEqual(bytesBuffer))
                {
                    return endTagNotFound;
                }
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, JPEG_COUNT_CHUNK_DATA_BYTES);
                while (true)
                {
                    // 读取 JPEG 数据块类型标记
                    if (this.InitialStream.Read(bytesBuffer, 0, JPEG_COUNT_CHUNK_DATA_BYTES) != JPEG_COUNT_CHUNK_DATA_BYTES)
                    {
                        return endTagNotFound;
                    }
                    // 判断是否是 JPEG 扫描开始标记
                    if (JPEG_SOS.ElementsEqual(bytesBuffer))
                    {
                        // 扫描段后面是压缩数据，无法简单获得数据长度
                        break;
                    }
                    // 读取 JPEG 数据块的数据域长度
                    if (this.InitialStream.Read(bytesBuffer, 0, JPEG_COUNT_CHUNK_DATA_BYTES) != JPEG_COUNT_CHUNK_DATA_BYTES)
                    {
                        return endTagNotFound;
                    }
                    // JPEG 内数据域长度是以大端字节序储存的，需要翻转
                    Array.Reverse(bytesBuffer, 0, JPEG_COUNT_CHUNK_DATA_BYTES);
                    int dataBlockLength = BitConverter.ToInt16(bytesBuffer, 0);
                    // 这个数据域长度是包括已经读取的数据域长度值本身在内的，所以移动指针的时候要减掉
                    this.InitialStream.Position += dataBlockLength - JPEG_COUNT_CHUNK_DATA_BYTES;
                }
                byte previousMarkLastByte = 0;
                while (true)
                {
                    long previousPosition = this.InitialStream.Position;
                    int actualReadCount = this.InitialStream.Read(bytesBuffer, 0, JPEG_COUNT_CHUNK_DATA_BYTES);
                    if (actualReadCount == 0)
                    {
                        return endTagNotFound;
                    }
                    if (previousMarkLastByte == JPEG_EOI[0] && bytesBuffer[0] == JPEG_EOI[1])
                    {
                        return previousPosition + 1;
                    }
                    if (actualReadCount == 2 && bytesBuffer[0] == JPEG_EOI[0] && bytesBuffer[1] == JPEG_EOI[1])
                    {
                        return previousPosition + 2;
                    }
                    previousMarkLastByte = bytesBuffer[actualReadCount - 1];
                }
            }
            catch (Exception)
            {
                return endTagNotFound;
            }
            finally
            {
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, 0);
            }
        }

        private FileDataInfo GetFileDataInformation(long infoStartingPos)
        {
            bool tailable = infoStartingPos != 0L;
            FileDataInfo defaultInfoForValidStream = new FileDataInfo(
                0L, tailable ? infoStartingPos : this.InitialStream.Length, tailable);
            if (this.ValidInitialStream)
            {
                byte[] buffer = null;
                try
                {
                    if (this.InitialStream.Length < HC_HEAD.Length || infoStartingPos < 0 ||
                        infoStartingPos > this.InitialStream.Length)
                    {
                        return defaultInfoForValidStream;
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, HC_HEAD.Length);
                    this.InitialStream.Position = infoStartingPos;
                    if (this.InitialStream.Read(buffer, 0, HC_HEAD.Length) != HC_HEAD.Length ||
                        !HC_HEAD.ElementsEqual(buffer))
                    {
                        return defaultInfoForValidStream;
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_NAME_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_NAME_LENGTH) != COUNT_BYTES_RECORD_NAME_LENGTH)
                    {
                        return defaultInfoForValidStream;
                    }
                    int algoNameBytesLength = buffer[0];
                    GlobalUtils.MakeSureBuffer(ref buffer, algoNameBytesLength);
                    if (this.InitialStream.Read(buffer, 0, algoNameBytesLength) != algoNameBytesLength)
                    {
                        return defaultInfoForValidStream;
                    }
                    string algoName;
                    try
                    {
                        algoName = Encoding.ASCII.GetString(buffer, 0, algoNameBytesLength);
                    }
                    catch (Exception)
                    {
                        return defaultInfoForValidStream;
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_HASH_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_HASH_LENGTH) != COUNT_BYTES_RECORD_HASH_LENGTH)
                    {
                        return defaultInfoForValidStream;
                    }
                    int hashValueBytesLength = BitConverter.ToInt16(buffer, 0);
                    // 不用 MakeSureBuffer 共享数组，因为要将数组返回给调用方
                    byte[] hashValueBytes = new byte[hashValueBytesLength];
                    if (this.InitialStream.Read(hashValueBytes, 0, hashValueBytesLength) != hashValueBytesLength)
                    {
                        return defaultInfoForValidStream;
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_RAND_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_RAND_LENGTH) != COUNT_BYTES_RECORD_RAND_LENGTH)
                    {
                        return defaultInfoForValidStream;
                    }
                    if (infoStartingPos != 0L)
                    {
                        return new FileDataInfo(0L, infoStartingPos, algoName, hashValueBytes, tailable);
                    }
                    else
                    {
                        long actualStartingPosition = this.InitialStream.Position + buffer[0];
                        return new FileDataInfo(actualStartingPosition, this.InitialStream.Length - actualStartingPosition,
                            algoName, hashValueBytes, tailable);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    GlobalUtils.MakeSureBuffer(ref buffer, 0);
                }
            }
            return default(FileDataInfo);
        }

        public bool TryGetFileDataInfo(out FileDataInfo information)
        {
            // 先当作头部有 HCM 标记的文件对待，找不到标记再当作 PNG/JPEG 文件对待
            information = this.GetFileDataInformation(0L);
            if (information != null && !information.IsTagged)
            {
                long infoStartingPosition = this.GetPngRealLength();
                if (infoStartingPosition == 0L)
                {
                    infoStartingPosition = this.GetJpegRealLength();
                }
                if (infoStartingPosition != 0L)
                {
                    information = this.GetFileDataInformation(infoStartingPosition);
                }
            }
            return information != null;
        }

        public bool TryGetTaggedFileDataInfo(out FileDataInfo information)
        {
            if (!this.TryGetFileDataInfo(out information))
            {
                return false;
            }
            else if (!information.IsTagged)
            {
                information = default(FileDataInfo);
                return false;
            }
            else
            {
                return information.IsTagged;
            }
        }

        private bool IsValidExternalStream(Stream stream)
        {
            return stream != this.InitialStream && stream != null && stream.CanWrite && stream.CanSeek;
        }

        private static bool IsValidAlgoModel(AlgoInOutModel algoModel)
        {
            return algoModel != null && algoModel.AlgoType != AlgoType.Unknown && algoModel.HashResult?.Length != 0;
        }

        private bool WriteHcmHeaderToStream(Stream stream, AlgoInOutModel algoModel)
        {
            try
            {
                byte[] algoNameBytes = Encoding.ASCII.GetBytes(algoModel.AlgoName);
                byte[] hashLengthBytes = BitConverter.GetBytes((short)algoModel.HashResult.Length);
                if (algoNameBytes?.Length > byte.MaxValue ||
                    algoModel.HashResult.Length > short.MaxValue ||
                    hashLengthBytes.Length != COUNT_BYTES_RECORD_HASH_LENGTH)
                {
                    return false;
                }
                Random randomGernerator = new Random();
                byte randomBytesLength = (byte)randomGernerator.Next(RANDOM_DATA_LENTGH_LOWER, RANDOM_DATA_LENTGH_UPPER);
                byte[] randomBytesBuffer = new byte[randomBytesLength];
                randomGernerator.NextBytes(randomBytesBuffer);
                stream.Write(HC_HEAD, 0, HC_HEAD.Length);
                stream.WriteByte((byte)algoNameBytes.Length);
                stream.Write(algoNameBytes, 0, algoNameBytes.Length);
                stream.Write(hashLengthBytes, 0, COUNT_BYTES_RECORD_HASH_LENGTH);
                stream.Write(algoModel.HashResult, 0, algoModel.HashResult.Length);
                stream.WriteByte(randomBytesLength);
                stream.Write(randomBytesBuffer, 0, randomBytesLength);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool WriteActualDataToStream(Stream stream, FileDataInfo info, DoubleProgressModel progressModel)
        {
            byte[] bytesBuffer = null;
            try
            {
                long dataCount;
                if (info == null)
                {
                    this.InitialStream.Position = 0L;
                    dataCount = this.InitialStream.Length;
                }
                else
                {
                    this.InitialStream.Position = info.ActualStart;
                    dataCount = info.ActualCount;
                }
                GlobalUtils.Suggest(ref bytesBuffer, dataCount);
                double progressValue = 0L;
                long remainCount = dataCount;
                int actualReadCount;
                int plannedReadCount = bytesBuffer.Length;
                while (remainCount > 0L)
                {
                    if (remainCount < bytesBuffer.Length)
                    {
                        plannedReadCount = (int)remainCount;
                    }
                    if ((actualReadCount = this.InitialStream.Read(bytesBuffer, 0, plannedReadCount)) == 0)
                    {
                        break;
                    }
                    stream.Write(bytesBuffer, 0, actualReadCount);
                    remainCount -= actualReadCount;
                    progressValue += actualReadCount;
                    progressModel.CurrentValue = progressValue / dataCount;
                }
                return remainCount == 0L;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, 0);
            }
        }

        /// <summary>
        /// 从带有哈希标记 (HCM标记) 的文件中还原出无标记的文件。
        /// </summary>
        public bool RestoreTaggedFile(Stream stream, FileDataInfo info, DoubleProgressModel progress)
        {
            if (this.ValidInitialStream && this.IsValidExternalStream(stream) &&
                info?.IsTagged == true)
            {
                byte[] buffer = null;
                try
                {
                    this.InitialStream.Position = info.ActualStart;
                    GlobalUtils.Suggest(ref buffer, info.ActualCount);
                    double progressValue = 0L;
                    long remainCount = info.ActualCount;
                    int actualReadCount = 0;
                    int plannedReadCount = buffer.Length;
                    while (remainCount > 0L)
                    {
                        if (remainCount < buffer.Length)
                        {
                            plannedReadCount = (int)remainCount;
                        }
                        if ((actualReadCount = this.InitialStream.Read(buffer, 0, plannedReadCount)) == 0)
                        {
                            break;
                        }
                        stream.Write(buffer, 0, actualReadCount);
                        remainCount -= actualReadCount;
                        progressValue += actualReadCount;
                        progress.CurrentValue = progressValue / info.ActualCount;
                    }
                    return remainCount == 0L;
                }
                catch (Exception)
                {
                }
                finally
                {
                    GlobalUtils.MakeSureBuffer(ref buffer, 0);
                }
            }
            return false;
        }

        /// <summary>
        /// 生成有哈希标记的文件，哈希标记 (HCM标记) 具体格式如下：<br/>
        /// 文件头(HC_HEAD)标识，1 字节算法名长度，算法名，2 字节算法结果长度，算法结果, 1 字节随机数据长度, 随机数据<br/>
        /// 对于 PNG/JPEG 文件，如果 tailTag 参数为 true，则程序会在其结束标记后写入【HCM标记】，否则在文件起始写入；<br/>
        /// 对于其他非 PNG/JPEG 文件，程序会在文件起始位置写入【HCM标记】。
        /// </summary>
        public bool GenerateTaggedFile(Stream stream, FileDataInfo info, AlgoInOutModel model, bool tailTag, DoubleProgressModel progress)
        {
            if (this.IsValidExternalStream(stream) && IsValidAlgoModel(model))
            {
                if (!tailTag || !info.IsTailable)
                {
                    return this.WriteHcmHeaderToStream(stream, model) &&
                        this.WriteActualDataToStream(stream, info, progress);
                }
                else
                {
                    return this.WriteActualDataToStream(stream, info, progress) &&
                        this.WriteHcmHeaderToStream(stream, model);
                }
            }
            return false;
        }
    }
}
