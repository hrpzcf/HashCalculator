using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

namespace HashCalculator
{
    internal static class CommonUtils
    {
        private const double kb = 1024D;
        private const double mb = 1048576D;
        private const double gb = 1073741824D;

        public static void Swap<T>(ref T leftValue, ref T rightValue)
        {
            T temp;
            temp = leftValue;
            leftValue = rightValue;
            rightValue = temp;
        }

        public static string FileSizeCvt(long bytesLength)
        {
            double byteLenInUnits;
            byteLenInUnits = bytesLength / gb;
            if (byteLenInUnits >= 1)
            {
                return $"{byteLenInUnits:f1} GB";
            }
            byteLenInUnits = bytesLength / mb;
            if (byteLenInUnits >= 1)
            {
                return $"{byteLenInUnits:f1} MB";
            }
            byteLenInUnits = bytesLength / kb;
            if (byteLenInUnits >= 1)
            {
                return $"{byteLenInUnits:f1} KB";
            }
            return $"{bytesLength} B";
        }

        public static bool OpenFolderAndSelectItem(string path)
        {
            if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
            {
                return false;
            }
            path = path.Replace("/", "\\");
            NativeFunctions.SHParseDisplayName(
                path, IntPtr.Zero, out IntPtr nativePath, 0U, out _);
            if (nativePath == IntPtr.Zero)
            {
                return false;
            }
            int intResult = NativeFunctions.SHOpenFolderAndSelectItems(nativePath, 0U, null, 0U);
            Marshal.FreeCoTaskMem(nativePath);
            return 0 == intResult;
        }

        public static bool OpenFolderAndSelectItems(string folderPath, IEnumerable<string> files)
        {
            if (string.IsNullOrEmpty(folderPath)
                || !Path.IsPathRooted(folderPath)
                || !Directory.Exists(folderPath))
            {
                return false;
            }
            folderPath = folderPath.Replace("/", "\\");
            NativeFunctions.SHParseDisplayName(
                folderPath, IntPtr.Zero, out IntPtr folderID, 0U, out _);
            if (folderID == IntPtr.Zero)
            {
                return false;
            }
            if (files == null || !files.Any())
            {
                int intResult1 = NativeFunctions.SHOpenFolderAndSelectItems(folderID, 0U, null, 0U);
                Marshal.FreeCoTaskMem(folderID);
                return 0 == intResult1;
            }
            List<IntPtr> fileIDList = new List<IntPtr>();
            foreach (string fname in files)
            {
                string fileFullPath = Path.Combine(folderPath, fname);
                if (!File.Exists(fileFullPath))
                {
                    continue;
                }
                NativeFunctions.SHParseDisplayName(
                    fileFullPath, IntPtr.Zero, out IntPtr fileID, 0U, out _);
                if (fileID != null)
                {
                    fileIDList.Add(fileID);
                }
            }
            int intResult2 = 0;
            if (fileIDList.Any())
            {
                intResult2 = NativeFunctions.SHOpenFolderAndSelectItems(folderID, (uint)fileIDList.Count,
                   fileIDList.ToArray(), 0U);
            }
            Marshal.FreeCoTaskMem(folderID);
            foreach (IntPtr fileID in fileIDList)
            {
                Marshal.FreeCoTaskMem(fileID);
            }
            return 0 == intResult2;
        }

        public static string ToBase64String(byte[] bytesPassedIn)
        {
            if (bytesPassedIn is null)
            {
                return default;
            }
            return Convert.ToBase64String(bytesPassedIn);
        }

        public static byte[] FromBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
            {
                return default;
            }
            try
            {
                return Convert.FromBase64String(base64String);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static string ToHexStringUpper(byte[] bytesPassedIn)
        {
            return ToHexString(bytesPassedIn, "X2");
        }

        public static string ToHexStringLower(byte[] bytesPassedIn)
        {
            return ToHexString(bytesPassedIn, "x2");
        }

        private static string ToHexString(byte[] bytesPassedIn, string format)
        {
            Debug.Assert(new string[] { "x2", "X2" }.Contains(format));
            if (bytesPassedIn is null)
            {
                return default;
            }
            StringBuilder stringBuilder = new StringBuilder(bytesPassedIn.Length * 2);
            for (int i = 0; i < bytesPassedIn.Length; ++i)
            {
                stringBuilder.Append(bytesPassedIn[i].ToString(format));
            }
            return stringBuilder.ToString();
        }

        public static byte[] FromHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString) || hexString.Length % 2 != 0)
            {
                return default;
            }
            byte[] resultBytes = new byte[hexString.Length / 2];
            try
            {
                for (int i = 0; i < resultBytes.Length; ++i)
                {
                    resultBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }
            }
            catch (Exception)
            {
                return default;
            }
            return resultBytes;
        }

        public static byte[] HashFromAnyString(string hashString)
        {
            if (FromHexString(hashString) is byte[] bytesGuessFromHex)
            {
                return bytesGuessFromHex;
            }
            else if (FromBase64String(hashString) is byte[] bytesGuessFromBase64)
            {
                return bytesGuessFromBase64;
            }
            return default;
        }

        public static bool SendToRecycleBin(IntPtr hParent, string path, bool silent = true)
        {
            FILEOP_FLAGS flags = FILEOP_FLAGS.FOF_ALLOWUNDO;
            if (silent)
            {
                flags |= FILEOP_FLAGS.FOF_SILENT;
                flags |= FILEOP_FLAGS.FOF_NOCONFIRMATION;
            }
            int result;
            if (IntPtr.Size == 4)
            {
                SHFILEOPSTRUCTW32 data = new SHFILEOPSTRUCTW32
                {
                    hwnd = hParent,
                    wFunc = (uint)FileFuncFlags.FO_DELETE,
                    pFrom = path + '\0',
                    fFlags = (ushort)flags,
                };
                result = NativeFunctions.SHFileOperationW32(ref data);
            }
            else
            {
                SHFILEOPSTRUCTW64 data = new SHFILEOPSTRUCTW64
                {
                    hwnd = hParent,
                    wFunc = (uint)FileFuncFlags.FO_DELETE,
                    pFrom = path + '\0',
                    fFlags = (ushort)flags,
                };
                result = NativeFunctions.SHFileOperationW64(ref data);
            }
            return result == 0;
        }

        public static bool IsPointToSameFile(string filePath1, string filePath2, out bool isSameFile)
        {
            int INVALID_HANDLE_VALUE = -1;
            IntPtr fileHandle1 = new IntPtr(INVALID_HANDLE_VALUE);
            IntPtr fileHandle2 = new IntPtr(INVALID_HANDLE_VALUE);
            bool executeResult = false;
            isSameFile = false;
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                goto FinalizeAndReturnResult;
            }
            fileHandle1 = NativeFunctions.CreateFileW(filePath1, 0U, FileShare.Read | FileShare.Write | FileShare.Delete,
               IntPtr.Zero, FileMode.Open, FileAttributes.Normal | FileAttributes.ReparsePoint, IntPtr.Zero);
            if (fileHandle1.ToInt32() == INVALID_HANDLE_VALUE)
            {
                goto FinalizeAndReturnResult;
            }
            fileHandle2 = NativeFunctions.CreateFileW(filePath2, 0U, FileShare.Read | FileShare.Write | FileShare.Delete,
               IntPtr.Zero, FileMode.Open, FileAttributes.Normal | FileAttributes.ReparsePoint, IntPtr.Zero);
            if (fileHandle2.ToInt32() == INVALID_HANDLE_VALUE)
            {
                goto FinalizeAndReturnResult;
            }
            if (!NativeFunctions.GetFileInformationByHandle(fileHandle1, out BY_HANDLE_FILE_INFORMATION fileInfo1) ||
                !NativeFunctions.GetFileInformationByHandle(fileHandle2, out BY_HANDLE_FILE_INFORMATION fileInfo2))
            {
                goto FinalizeAndReturnResult;
            }
            isSameFile = fileInfo1.dwVolumeSerialNumber == fileInfo2.dwVolumeSerialNumber &&
                fileInfo1.nFileIndexLow == fileInfo2.nFileIndexLow && fileInfo1.nFileIndexHigh == fileInfo2.nFileIndexHigh;
            executeResult = true;
        FinalizeAndReturnResult:
            if (fileHandle1.ToInt32() != INVALID_HANDLE_VALUE)
            {
                NativeFunctions.CloseHandle(fileHandle1);
            }
            if (fileHandle2.ToInt32() != INVALID_HANDLE_VALUE)
            {
                NativeFunctions.CloseHandle(fileHandle2);
            }
            return executeResult;
        }

        private class CyclingDouble : IEnumerable<double>
        {
            private readonly double _maxValue;
            private readonly double _minValue;
            private readonly int _count;
            private readonly float _increments;

            public CyclingDouble(double minValue, double maxValue, int count)
            {
                this._minValue = minValue;
                this._maxValue = maxValue;
                if (count % 2 == 0)
                {
                    this._count = count;
                }
                else
                {
                    this._count = count + 1;
                }
                this._increments = (float)(maxValue / this._count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public IEnumerator<double> GetEnumerator()
            {
                Random random = new Random();
                double[] doubleHues = new double[this._count];
                // 色环随机起点，避免每次都是 0° (正红) 为起点
                double start = (random.NextDouble() * this._maxValue) + this._minValue;
                double splitPoint = start;
                int index = 0;
                while (start < this._maxValue && index < this._count)
                {
                    doubleHues[index++] = start;
                    start += this._increments;
                }
                if (splitPoint > this._minValue)
                {
                    start = start - this._maxValue + this._minValue;
                    while (start < splitPoint && index < this._count)
                    {
                        doubleHues[index++] = start;
                        start += this._increments;
                    }
                }
                index = 0;
                int middle = this._count / 2;
                int iBack = middle;
                bool arrayFrontEnd = true;
                while (index < middle || iBack < this._count)
                {
                    yield return arrayFrontEnd ? doubleHues[index++] : doubleHues[iBack++];
                    arrayFrontEnd = !arrayFrontEnd;
                }
            }
        }

        private static Color RgbDwordToColor(uint color)
        {
            // DWORD from ColorHLSToRGB: 0x00bbggrr(RGB)
            return Color.FromRgb((byte)(color & 0xFFu), (byte)((color & 0xFF00u) >> 8),
                (byte)((color & 0xFF0000u) >> 16));
        }

        public static Color[] ColorGenerator(int number)
        {
            List<Color> colors = new List<Color>(number);
            // 函数 ColorHLSToRGB 三个参数范围都是 0~240
            double MAX_HLS = 240.0;
            Random random = new Random();
            foreach (double H in new CyclingDouble(0.0, MAX_HLS, number))
            {
                int L = random.Next(170, 190);
                int S = random.Next(120, 180);
                colors.Add(RgbDwordToColor(NativeFunctions.ColorHLSToRGB((int)H, L, S)));
            }
            return colors.ToArray();
        }
    }
}
