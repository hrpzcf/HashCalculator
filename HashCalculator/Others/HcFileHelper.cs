using System;
using System.IO;
using System.Text;

namespace HashCalculator
{
    /// <summary>
    /// 用于检测文件是否是由本程序生成的有哈希标记的文件、生成有哈希标记的文件、从有哈希标记的文件还原出原文件。<br/>
    /// </summary>
    internal class HcFileHelper : IDisposable
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
        /// 对于 PNG/JPEG 文件来说也可能放在尾部，取决于调用 GenerateHcFile 方法时的 senseFree 参数。
        /// </summary>
        private static readonly byte[] HC_HEAD =
            { 0xF3, 0x48, 0x43, 0x4D, 0x20, 0x66, 0x69, 0x6C, 0x65 };

        private Stream InitialStream { get; }

        private long InitialPosition { get; } = -1L;

        public HcFileHelper(Stream stream)
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

        public bool ValidInitialStream
        {
            get
            {
                return this.InitialStream != null &&
                    this.InitialStream.CanRead && !this.InitialStream.CanWrite && this.InitialStream.CanSeek;
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

        private bool IsUsableExternalStream(Stream stream)
        {
            return stream != this.InitialStream && stream != null && stream.CanWrite && stream.CanSeek;
        }

        private static bool IsUsableAlgoModel(AlgoInOutModel algoModel)
        {
            return algoModel != null && algoModel.AlgoType != AlgoType.Unknown && algoModel.HashResult?.Length != 0;
        }

        private void WriteHcHeaderToStream(Stream stream, AlgoInOutModel algoModel)
        {
            try
            {
                byte[] algoNameBytes = Encoding.ASCII.GetBytes(algoModel.AlgoName);
                if (algoNameBytes?.Length > byte.MaxValue)
                {
                    return;
                }
                if (algoModel.HashResult.Length > short.MaxValue)
                {
                    return;
                }
                byte[] hashResultLengthBytes = BitConverter.GetBytes((short)algoModel.HashResult.Length);
                if (hashResultLengthBytes.Length != COUNT_BYTES_RECORD_HASH_LENGTH)
                {
                    return;
                }
                Random randomGernerator = new Random();
                byte randomBytesLength = (byte)randomGernerator.Next(RANDOM_DATA_LENTGH_LOWER, RANDOM_DATA_LENTGH_UPPER);
                byte[] randomBytesBuffer = new byte[randomBytesLength];
                randomGernerator.NextBytes(randomBytesBuffer);
                stream.Write(HC_HEAD, 0, HC_HEAD.Length);
                stream.WriteByte((byte)algoNameBytes.Length);
                stream.Write(algoNameBytes, 0, algoNameBytes.Length);
                stream.Write(hashResultLengthBytes, 0, COUNT_BYTES_RECORD_HASH_LENGTH);
                stream.Write(algoModel.HashResult, 0, algoModel.HashResult.Length);
                stream.WriteByte(randomBytesLength);
                stream.Write(randomBytesBuffer, 0, randomBytesLength);
            }
            catch (Exception)
            {
            }
        }

        private void WriteRealDataToStream(Stream stream, HcFileInfo info, DoubleProgressModel progressModel)
        {
            long dataStart, dataCount;
            if (info == null)
            {
                dataStart = 0L;
                dataCount = this.InitialStream.Length;
            }
            else
            {
                dataStart = info.RealStart;
                dataCount = info.RealCount;
            }
            byte[] buffer = null;
            try
            {
                long remainCount = dataCount;
                double progressValue = 0L;
                GlobalUtils.Suggest(ref buffer, dataCount);
                int plannedReadCount = buffer.Length;
                this.InitialStream.Position = dataStart;
                while (remainCount > 0L)
                {
                    if (remainCount < buffer.Length)
                    {
                        plannedReadCount = (int)remainCount;
                    }
                    int actualReadCount = this.InitialStream.Read(buffer, 0, plannedReadCount);
                    stream.Write(buffer, 0, actualReadCount);
                    remainCount -= actualReadCount;
                    progressValue += actualReadCount;
                    progressModel.ProgressValue = progressValue / dataCount;
                }
            }
            finally
            {
                GlobalUtils.MakeSureBuffer(ref buffer, 0);
            }
        }

        /// <summary>
        /// 生成有哈希标记的文件，哈希标记 (下称【HCM标记】) 具体格式如下：<br/>
        /// 文件头(HC_HEAD)标识，1 字节算法名长度，算法名，2 字节算法结果长度，算法结果, 1 字节随机数据长度, 随机数据<br/>
        /// 对于 JPEG 文件，如果 senseFree 参数为 true，则程序会在 0xFFD9 标记后写入【HCM标记】，否则在文件起始写入；<br/>
        /// 对于 PNG 文件，如果 senseFree 参数为 true，则程序会在 0x49454E44AE426082 标记后写入【HCM标记】，否则在文件起始写入；<br/>
        /// 对于其他非 JPEG 文件和非 PNG 文件，程序会在文件起始位置写入【HCM标记】。
        /// </summary>
        public bool GenerateHcFile(Stream stream, AlgoInOutModel model, bool senseFree,
            DoubleProgressModel doubleProgressModel)
        {
            if (IsUsableAlgoModel(model) && this.ValidInitialStream && this.IsUsableExternalStream(stream))
            {
                long hcInfoStartPosition = 0L;
                if (senseFree)
                {
                    hcInfoStartPosition = this.GetPngRealLength();
                    if (hcInfoStartPosition == 0L)
                    {
                        hcInfoStartPosition = this.GetJpegRealLength();
                    }
                }
                HcFileInfo information = this.GetHcFileInformation(hcInfoStartPosition);
                long previousPosition = -1L;
                try
                {
                    previousPosition = stream.Position;
                    stream.Position = 0L;
                    if (hcInfoStartPosition != 0L)
                    {
                        this.WriteRealDataToStream(stream, information, doubleProgressModel);
                        this.WriteHcHeaderToStream(stream, model);
                    }
                    else
                    {
                        this.WriteHcHeaderToStream(stream, model);
                        this.WriteRealDataToStream(stream, information, doubleProgressModel);
                    }
                    return true;
                }
                finally
                {
                    if (previousPosition != -1L)
                    {
                        stream.Position = previousPosition;
                    }
                }
            }
            return false;
        }

        public bool RestoreHcFile(Stream stream, DoubleProgressModel progressModel)
        {
            if (this.ValidInitialStream && this.IsUsableExternalStream(stream))
            {
                // 优先从头部查找 HCM 标记
                HcFileInfo information = this.GetHcFileInformation(0L);
                if (information == null)
                {
                    long hcInfoStartPosition = this.GetPngRealLength();
                    if (hcInfoStartPosition == 0L)
                    {
                        hcInfoStartPosition = this.GetJpegRealLength();
                    }
                    if (hcInfoStartPosition != 0L)
                    {
                        information = this.GetHcFileInformation(hcInfoStartPosition);
                    }
                }
                if (information != null)
                {
                    double progressValue = 0L;
                    long initialPosition = -1L;
                    long remainCount = information.RealCount;
                    byte[] buffer = null;
                    try
                    {
                        GlobalUtils.Suggest(ref buffer, information.RealCount);
                        initialPosition = stream.Position;
                        int plannedReadCount = buffer.Length;
                        stream.Position = information.RealStart;
                        while (remainCount > 0L)
                        {
                            if (remainCount < buffer.Length)
                            {
                                plannedReadCount = (int)remainCount;
                            }
                            int actualReadCount = this.InitialStream.Read(buffer, 0, plannedReadCount);
                            stream.Write(buffer, 0, actualReadCount);
                            remainCount -= actualReadCount;
                            progressValue += actualReadCount;
                            progressModel.ProgressValue = progressValue / information.RealCount;
                        }
                        return true;
                    }
                    finally
                    {
                        if (initialPosition != -1L)
                        {
                            stream.Position = initialPosition;
                        }
                        GlobalUtils.MakeSureBuffer(ref buffer, 0);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取 PNG 文件的真实大小(不包含 0x49 45 4E 44 AE 42 60 82 标记后的内容)，
        /// 如果返回 0 则代表不是 PNG 文件。
        /// </summary>
        public long GetPngRealLength()
        {
            byte[] bytesBuffer = null;
            const long endTagNotFound = 0L;
            if (!this.ValidInitialStream)
            {
                return endTagNotFound;
            }
            try
            {
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
            finally
            {
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, 0);
            }
        }

        /// <summary>
        /// 获取 JPEG 文件的真实大小(不包含 0xFF D9 标记后的内容)，如果返回 0 则代表不是 JPEG 文件。
        /// </summary>
        public long GetJpegRealLength()
        {
            byte[] bytesBuffer = null;
            const long endTagNotFound = 0L;
            if (!this.ValidInitialStream)
            {
                return endTagNotFound;
            }
            try
            {
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
            finally
            {
                GlobalUtils.MakeSureBuffer(ref bytesBuffer, 0);
            }
        }

        /// <summary>
        /// 获取被本程序修改过哈希值的文件的信息 (原算法名、原哈希值、文件的真实起始位置、真实字节数)。<br/>
        /// 如果此方法返回 null 则代表该文件流所指文件不是被本程序修改过哈希值的文件。<br/>
        /// 参数 expHcInfoStart 是期望的 HcHead 起始位置，如果是 JPEG 文件则为 0xFFD9 的后一位，不是则为 0。
        /// </summary>
        public HcFileInfo GetHcFileInformation(long expHcInfoStart)
        {
            if (this.ValidInitialStream)
            {
                byte[] buffer = null;
                try
                {
                    if (this.InitialStream.Length < HC_HEAD.Length || expHcInfoStart < 0 ||
                        expHcInfoStart > this.InitialStream.Length)
                    {
                        return default(HcFileInfo);
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, HC_HEAD.Length);
                    this.InitialStream.Position = expHcInfoStart;
                    if (this.InitialStream.Read(buffer, 0, HC_HEAD.Length) != HC_HEAD.Length ||
                        !HC_HEAD.ElementsEqual(buffer))
                    {
                        return default(HcFileInfo);
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_NAME_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_NAME_LENGTH) != COUNT_BYTES_RECORD_NAME_LENGTH)
                    {
                        return default(HcFileInfo);
                    }
                    int algoNameBytesLength = buffer[0];
                    GlobalUtils.MakeSureBuffer(ref buffer, algoNameBytesLength);
                    if (this.InitialStream.Read(buffer, 0, algoNameBytesLength) != algoNameBytesLength)
                    {
                        return default(HcFileInfo);
                    }
                    string algoName;
                    try
                    {
                        algoName = Encoding.ASCII.GetString(buffer, 0, algoNameBytesLength);
                    }
                    catch (Exception)
                    {
                        return default(HcFileInfo);
                    }
                    if (!Enum.TryParse(algoName, true, out AlgoType algoType))
                    {
                        return default(HcFileInfo);
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_HASH_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_HASH_LENGTH) != COUNT_BYTES_RECORD_HASH_LENGTH)
                    {
                        return default(HcFileInfo);
                    }
                    int hashValueBytesLength = BitConverter.ToInt16(buffer, 0);
                    // 不用 MakeSureBuffer 共享数组，因为要将数组返回给调用方
                    byte[] hashValueBytes = new byte[hashValueBytesLength];
                    if (this.InitialStream.Read(hashValueBytes, 0, hashValueBytesLength) != hashValueBytesLength)
                    {
                        return default(HcFileInfo);
                    }
                    GlobalUtils.MakeSureBuffer(ref buffer, COUNT_BYTES_RECORD_RAND_LENGTH);
                    if (this.InitialStream.Read(buffer, 0, COUNT_BYTES_RECORD_RAND_LENGTH) != COUNT_BYTES_RECORD_RAND_LENGTH)
                    {
                        return default(HcFileInfo);
                    }
                    if (expHcInfoStart != 0L)
                    {
                        return new HcFileInfo(algoType, hashValueBytes, 0L, expHcInfoStart);
                    }
                    else
                    {
                        long streamRealStart = this.InitialStream.Position + buffer[0];
                        return new HcFileInfo(algoType, hashValueBytes, streamRealStart,
                            this.InitialStream.Length - streamRealStart);
                    }
                }
                finally
                {
                    GlobalUtils.MakeSureBuffer(ref buffer, 0);
                }
            }
            return default(HcFileInfo);
        }
    }
}
