using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal static class CommonUtils
    {
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
#if DEBUG
                Console.WriteLine("OpenFolderAndSelectItem failed");
#endif
                return false;
            }
            int res = NativeFunctions.SHOpenFolderAndSelectItems(nativePath, 0U, null, 0U);
            Marshal.FreeCoTaskMem(nativePath);
            return res == 0;
        }

        public static bool OpenFolderAndSelectItems(string folderPath, string[] files)
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
#if DEBUG
                Console.WriteLine("OpenFolderAndSelectItem failed");
#endif
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
    }
}
