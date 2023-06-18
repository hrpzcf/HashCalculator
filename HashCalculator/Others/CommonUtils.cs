using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HashCalculator
{
    internal static class CommonUtils
    {
        private const double kb = 1024D;
        private const double mb = 1048576D;
        private const double gb = 1073741824D;

        public static string FileSizeCvt(long bytes)
        {
            double bytesto;
            bytesto = bytes / gb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}GB";
            }
            bytesto = bytes / mb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}MB";
            }
            bytesto = bytes / kb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}KB";
            }
            return $"{bytes}B";
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
            int res = NativeFunctions.SHOpenFolderAndSelectItems(nativePath, 0U, null, 0U);
            Marshal.FreeCoTaskMem(nativePath);
            return res == 0;
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
                int res1 = NativeFunctions.SHOpenFolderAndSelectItems(folderID, 0U, null, 0U);
                Marshal.FreeCoTaskMem(folderID);
                return res1 == 0;
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
            int res2 = NativeFunctions.SHOpenFolderAndSelectItems(folderID, (uint)fileIDList.Count,
                fileIDList.ToArray(), 0U);
            Marshal.FreeCoTaskMem(folderID);
            foreach (IntPtr fileID in fileIDList)
            {
                Marshal.FreeCoTaskMem(fileID);
            }
            return res2 == 0;
        }

        //public static bool IsSameFile(string path1, string path2)
        //{
        //    if (path1 is null || path2 is null)
        //    {
        //        return false;
        //    }
        //    using (SafeFileHandle handle1 = NativeFunctions.CreateFileW(
        //        path1, FileAccess.Read,
        //        FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
        //    {
        //        if (handle1.IsInvalid)
        //        {
        //            return false;
        //        }
        //        using (SafeFileHandle handle2 = NativeFunctions.CreateFileW(
        //            path2, FileAccess.Read,
        //            FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
        //        {
        //            if (handle2.IsInvalid)
        //            {
        //                return false;
        //            }
        //            bool result1 = NativeFunctions.GetFileInformationByHandle(
        //                handle1, out BY_HANDLE_FILE_INFORMATION information1);
        //            if (!result1)
        //            {
        //                return result1;
        //            }
        //            bool result2 = NativeFunctions.GetFileInformationByHandle(
        //                handle2, out BY_HANDLE_FILE_INFORMATION information2);
        //            if (!result2)
        //            {
        //                return result2;
        //            }
        //            return information1.VolumeSerialNumber == information2.VolumeSerialNumber
        //                && information1.FileIndexLow == information2.FileIndexLow
        //                && information1.FileIndexHigh == information2.FileIndexHigh;
        //        }
        //    }
        //}

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
            if (base64String is null)
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
            Debug.Assert(new string[] { "x2", "X2"}.Contains(format));
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
            if (hexString is null || hexString.Length % 2 != 0)
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

        public static bool SendToRecycleBin(
            IntPtr hParent, string path, bool silent = true)
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
    }
}
