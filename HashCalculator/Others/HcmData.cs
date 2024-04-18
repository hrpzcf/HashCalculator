using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HashCalculator
{
    internal class HcmData
    {
        #region HcmInfo
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct HcmInfo
        {
            /// <summary>
            /// 本程序定义的 HCM 标记
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] HcMarker;

            /// <summary>
            /// 算法名(ASCII 编码字节数组)长度
            /// </summary>
            public byte NameLength;

            /// <summary>
            /// 哈希值(字节数组)的长度
            /// </summary>
            public ushort HashLength;

            /// <summary>
            /// 随机数据(字节数组)的长度
            /// </summary>
            public byte RandomLength;

            /// <summary>
            /// 关键字段的 CRC32 校验和
            /// </summary>
            public uint CheckSumValue;

            public static int UnmanagedSize => Marshal.SizeOf(typeof(HcmInfo));

            /// <summary>
            /// 读取到的 HcmInfo 所指示的哈希标记预期长度
            /// </summary>
            public int DataLength => UnmanagedSize + this.NameLength +
                this.HashLength + this.RandomLength;

            public bool TryGetBytes(out byte[] structureBytes)
            {
                IntPtr structurePointer = IntPtr.Zero;
                try
                {
                    structurePointer = Marshal.AllocHGlobal(UnmanagedSize);
                    Marshal.StructureToPtr(this, structurePointer, false);
                    structureBytes = new byte[UnmanagedSize];
                    Marshal.Copy(structurePointer, structureBytes, 0, UnmanagedSize);
                    return true;
                }
                catch (Exception) { }
                finally
                {
                    if (structurePointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(structurePointer);
                    }
                }
                structureBytes = null;
                return false;
            }

            public static bool TryParse(byte[] bytes, out object hcmInfo)
            {
                IntPtr structurePointer = IntPtr.Zero;
                try
                {
                    Type type = typeof(HcmInfo);
                    if (bytes?.Length == UnmanagedSize)
                    {
                        structurePointer = Marshal.AllocHGlobal(bytes.Length);
                        Marshal.Copy(bytes, 0, structurePointer, bytes.Length);
                        hcmInfo = Marshal.PtrToStructure(structurePointer, type);
                        return true;
                    }
                }
                catch (Exception) { }
                finally
                {
                    if (structurePointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(structurePointer);
                    }
                }
                hcmInfo = default(object);
                return false;
            }

            public bool IsHcmDataMarker()
            {
                return this.HcMarker != null && this.HcMarker.SequenceEqual(MARKER);
            }
        }
        #endregion

        private string name = null;
        private byte[] nameBytes = null;
        private byte[] hash = null;
        private byte[] hashBytes = null;
        private byte[] randomData = null;
        private byte[] randomBytes = null;
        private HcmInfo hcmInfo;
        private bool readFromFile = false;
        private readonly byte[] separator = { 0x0A, 0x00 };
        private readonly int randomLower = 2;
        private readonly int randomUpper = 8;

        private static readonly Random random = new Random();

        /// <summary>
        /// 本程序自定义的 HCM 文件头，用于标记文件含有哈希值标记。
        /// </summary>
        private static readonly byte[] MARKER = { 0xF3, 0x48, 0x43, 0x4D };

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint crc32_update(byte[] input, ulong in_len, uint prevCrc32);

        [DllImport(Settings.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint crc32_update(ref byte input, ulong in_len, uint prevCrc32);

        public HcmData(long position, bool marker = false)
        {
            this.Initialize(marker);
            this.Position = position;
        }

        /// <summary>
        /// 指示本实例数据是否已被全部填充
        /// </summary>
        public bool Populated =>
            this.hcmInfo.NameLength != 0 &&
            this.hcmInfo.HashLength != 0 &&
            this.hcmInfo.RandomLength != 0 &&
            this.hcmInfo.IsHcmDataMarker() &&
            this.nameBytes != null &&
            this.hashBytes != null &&
            this.randomBytes != null &&
            this.hcmInfo.DataLength == HcmInfo.UnmanagedSize +
                this.nameBytes.Length + this.hashBytes.Length +
                this.randomBytes.Length;

        /// <summary>
        /// 指示本实例内的数据是否可靠：<br/>
        /// 如果数据是从文件读取，则检查数据是否非空以及关键字段校验和是否与记录的校验和一致；<br/>
        /// 如果该实例的数据不是从文件读取的，则检查数据是否全部非空。
        /// </summary>
        public bool DataReliable
        {
            get
            {
                if (this.readFromFile)
                {
                    return this.Populated &&
                        this.hcmInfo.CheckSumValue != 0u &&
                        this.TryGetKeyFieldsHash(out uint hash) &&
                        hash == this.hcmInfo.CheckSumValue;
                }
                else
                {
                    return this.Populated;
                }
            }
        }

        public long Position { get; private set; }

        public string Name
        {
            get
            {
                if (this.name == null)
                {
                    try
                    {
                        this.name = Encoding.ASCII.GetString(
                            this.nameBytes.FromBytesWithLargerValue());
                    }
                    catch (Exception)
                    {
                    }
                }
                return this.name;
            }
        }

        public byte[] Hash
        {
            get
            {
                if (this.hash == null)
                {
                    this.hash = this.hashBytes.FromBytesWithLargerValue();
                }
                return this.hash;
            }
        }

        public byte[] RandomData
        {
            get
            {
                if (this.randomData == null)
                {
                    this.randomData = this.randomBytes.FromBytesWithLargerValue();
                }
                return this.randomData;
            }
        }

        public void Initialize(bool marker)
        {
            this.hcmInfo = new HcmInfo();
            if (marker)
            {
                this.hcmInfo.HcMarker = MARKER;
            }
            this.Position = 0;
            this.name = null;
            this.nameBytes = null;
            this.hash = null;
            this.hashBytes = null;
            this.randomData = null;
            this.randomBytes = null;
            this.readFromFile = false;
        }

        private bool TryGetKeyFieldsHash(out uint hash)
        {
            try
            {
                hash = 0u;
                hash = crc32_update(ref this.hcmInfo.NameLength, 1ul, hash);
                byte[] hashLengthBytes = BitConverter.GetBytes(this.hcmInfo.HashLength);
                hash = crc32_update(hashLengthBytes, (ulong)hashLengthBytes.Length, hash);
                hash = crc32_update(ref this.hcmInfo.RandomLength, 1ul, hash);
                hash = crc32_update(this.separator, (ulong)this.separator.Length, hash);
                hash = crc32_update(this.nameBytes, (ulong)this.nameBytes.Length, hash);
                hash = crc32_update(this.hashBytes, (ulong)this.hashBytes.Length, hash);
                return true;
            }
            catch (Exception)
            {
                hash = 0u;
                return false;
            }
        }

        public bool TryWriteDataToStream(Stream stream)
        {
            if (this.Populated && this.TryGetKeyFieldsHash(out uint hash))
            {
                this.hcmInfo.CheckSumValue = hash;
                if (this.hcmInfo.TryGetBytes(out byte[] hcmInfoBytes))
                {
                    try
                    {
                        stream.Seek(0L, SeekOrigin.End);
                        stream.Write(this.separator, 0, this.separator.Length);
                        stream.Write(this.nameBytes, 0, this.nameBytes.Length);
                        stream.Write(this.hashBytes, 0, this.hashBytes.Length);
                        stream.Write(this.randomBytes, 0, this.randomBytes.Length);
                        stream.Write(hcmInfoBytes, 0, hcmInfoBytes.Length);
                        return true;
                    }
                    catch (Exception) { }
                }
            }
            return false;
        }

        public bool TryReadDataFromStream(Stream stream)
        {
            try
            {
                if (stream.Length <= HcmInfo.UnmanagedSize)
                {
                    return false;
                }
                this.readFromFile = true;
                byte[] infoBytes = new byte[HcmInfo.UnmanagedSize];
                stream.Seek(-infoBytes.Length, SeekOrigin.End);
                if (stream.Read(infoBytes, 0, infoBytes.Length) != infoBytes.Length)
                {
                    return false;
                }
                if (!HcmInfo.TryParse(infoBytes, out object obj) || !(obj is HcmInfo info) ||
                    !info.IsHcmDataMarker() || stream.Length <= info.DataLength)
                {
                    return false;
                }
                this.hcmInfo = info;
                stream.Seek(-info.DataLength, SeekOrigin.End);
                this.name = null;
                this.nameBytes = new byte[info.NameLength];
                if (stream.Read(this.nameBytes, 0, info.NameLength) != info.NameLength)
                {
                    return false;
                }
                this.hash = null;
                this.hashBytes = new byte[info.HashLength];
                if (stream.Read(this.hashBytes, 0, info.HashLength) != info.HashLength)
                {
                    return false;
                }
                this.randomData = null;
                this.randomBytes = new byte[info.RandomLength];
                if (stream.Read(this.randomBytes, 0, info.RandomLength) != info.RandomLength)
                {
                    return false;
                }
                this.Position = stream.Length - info.DataLength - this.separator.Length;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RefreshRandomData()
        {
            int length = random.Next(this.randomLower, this.randomUpper);
            this.randomBytes = new byte[length * 2];
            this.hcmInfo.RandomLength = (byte)this.randomBytes.Length;
            for (int i = 0; i < this.randomBytes.Length; ++i)
            {
                this.randomBytes[i] = (byte)random.Next(0xF0, byte.MaxValue + 1);
            }
            this.readFromFile = false;
        }

        public bool TrySetNameBytes(string name)
        {
            try
            {
                return this.TrySetNameBytes(Encoding.ASCII.GetBytes(name));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TrySetNameBytes(byte[] bytes)
        {
            if (bytes?.Length > 0)
            {
                byte[] largerValues = bytes.ToBytesWithLargerValue();
                if (CommonUtils.TryAssignin(largerValues.Length, ref this.hcmInfo.NameLength))
                {
                    this.name = null;
                    this.nameBytes = largerValues;
                    this.readFromFile = false;
                    return true;
                }
            }
            return false;
        }

        public bool TrySetHashBytes(byte[] bytes)
        {
            if (bytes?.Length > 0)
            {
                byte[] largerValues = bytes.ToBytesWithLargerValue();
                if (CommonUtils.TryAssignin(largerValues.Length, ref this.hcmInfo.HashLength))
                {
                    this.hash = null;
                    this.hashBytes = largerValues;
                    this.readFromFile = false;
                    return true;
                }
            }
            return false;
        }

        public bool TrySetRandomBytes(byte[] bytes)
        {
            if (bytes?.Length > 0)
            {
                byte[] largerValues = bytes.ToBytesWithLargerValue();
                if (CommonUtils.TryAssignin(largerValues.Length, ref this.hcmInfo.RandomLength))
                {
                    this.randomData = null;
                    this.randomBytes = largerValues;
                    this.readFromFile = false;
                    return true;
                }
            }
            return false;
        }
    }
}
