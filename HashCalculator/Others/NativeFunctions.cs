using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHFILEOPSTRUCTW64
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszProgressTitle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct SHFILEOPSTRUCTW32
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszProgressTitle;
    }

    public enum FileFuncFlags : uint
    {
        FO_MOVE = 0x1,
        FO_COPY = 0x2,
        FO_DELETE = 0x3,
        FO_RENAME = 0x4
    }

    [Flags]
    public enum FILEOP_FLAGS : ushort
    {
        FOF_MULTIDESTFILES = 0x1,
        FOF_CONFIRMMOUSE = 0x2,
        /// <summary>
        /// Don't create progress/report
        /// </summary>
        FOF_SILENT = 0x4,
        FOF_RENAMEONCOLLISION = 0x8,
        /// <summary>
        /// Don't prompt the user.
        /// </summary>
        FOF_NOCONFIRMATION = 0x10,
        /// <summary>
        /// Fill in SHFILEOPSTRUCT.hNameMappings.
        /// Must be freed using SHFreeNameMappings
        /// </summary>
        FOF_WANTMAPPINGHANDLE = 0x20,
        FOF_ALLOWUNDO = 0x40,
        /// <summary>
        /// On *.*, do only files
        /// </summary>
        FOF_FILESONLY = 0x80,
        /// <summary>
        /// Don't show names of files
        /// </summary>
        FOF_SIMPLEPROGRESS = 0x100,
        /// <summary>
        /// Don't confirm making any needed dirs
        /// </summary>
        FOF_NOCONFIRMMKDIR = 0x200,
        /// <summary>
        /// Don't put up error UI
        /// </summary>
        FOF_NOERRORUI = 0x400,
        /// <summary>
        /// Dont copy NT file Security Attributes
        /// </summary>
        FOF_NOCOPYSECURITYATTRIBS = 0x800,
        /// <summary>
        /// Don't recurse into directories.
        /// </summary>
        FOF_NORECURSION = 0x1000,
        /// <summary>
        /// Don't operate on connected elements.
        /// </summary>
        FOF_NO_CONNECTED_ELEMENTS = 0x2000,
        /// <summary>
        /// During delete operation,
        /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
        /// </summary>
        FOF_WANTNUKEWARNING = 0x4000,
        /// <summary>
        /// Treat reparse points as objects, not containers
        /// </summary>
        FOF_NORECURSEREPARSE = 0x8000
    }

    public enum ShowCmds : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11,
        SW_MAX = 11
    }

    [Flags]
    public enum SEMaskFlags : uint
    {
        SEE_MASK_DEFAULT = 0x00000000,
        SEE_MASK_CLASSNAME = 0x00000001,
        SEE_MASK_CLASSKEY = 0x00000003,
        SEE_MASK_IDLIST = 0x00000004,
        SEE_MASK_INVOKEIDLIST = 0x0000000c,   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04)
        SEE_MASK_HOTKEY = 0x00000020,
        SEE_MASK_NOCLOSEPROCESS = 0x00000040,
        SEE_MASK_CONNECTNETDRV = 0x00000080,
        SEE_MASK_NOASYNC = 0x00000100,
        SEE_MASK_FLAG_DDEWAIT = SEE_MASK_NOASYNC,
        SEE_MASK_DOENVSUBST = 0x00000200,
        SEE_MASK_FLAG_NO_UI = 0x00000400,
        SEE_MASK_UNICODE = 0x00004000,
        SEE_MASK_NO_CONSOLE = 0x00008000,
        SEE_MASK_ASYNCOK = 0x00100000,
        SEE_MASK_HMONITOR = 0x00200000,
        SEE_MASK_NOZONECHECKS = 0x00800000,
        SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
        SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
        SEE_MASK_FLAG_LOG_USAGE = 0x04000000,
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfow
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SHELLEXECUTEINFOW
    {
        public int cbSize;
        public SEMaskFlags fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDirectory;
        public ShowCmds nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    ///// <summary>
    ///// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information
    ///// </summary>
    //[StructLayout(LayoutKind.Explicit)]
    //public struct BY_HANDLE_FILE_INFORMATION
    //{
    //    [FieldOffset(0)]
    //    public uint FileAttributes;

    //    [FieldOffset(4)]
    //    public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;

    //    [FieldOffset(12)]
    //    public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;

    //    [FieldOffset(20)]
    //    public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;

    //    [FieldOffset(28)]
    //    public uint VolumeSerialNumber;

    //    [FieldOffset(32)]
    //    public uint FileSizeHigh;

    //    [FieldOffset(36)]
    //    public uint FileSizeLow;

    //    [FieldOffset(40)]
    //    public uint NumberOfLinks;

    //    [FieldOffset(44)]
    //    public uint FileIndexHigh;

    //    [FieldOffset(48)]
    //    public uint FileIndexLow;
    //}

    internal static class NativeFunctions
    {
        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems
        /// </summary>
        /// <param name="pidlFolder"></param>
        /// <param name="cidl"></param>
        /// <param name="apidl"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bindingContext"></param>
        /// <param name="pidl"></param>
        /// <param name="sfgaoIn"></param>
        /// <param name="psfgaoOut"></param>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern void SHParseDisplayName(
            string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecutew
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lpOperation"></param>
        /// <param name="lpFile"></param>
        /// <param name="lpParameters"></param>
        /// <param name="lpDirectory"></param>
        /// <param name="nShowCmd"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ShellExecuteW(
            IntPtr hwnd, string lpOperation, string lpFile,
            string lpParameters, string lpDirectory, ShowCmds nShowCmd);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw
        /// </summary>
        /// <param name="lpExecInfo"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        ///// <summary>
        ///// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle
        ///// </summary>
        ///// <param name="hFile"></param>
        ///// <param name="lpFileInformation"></param>
        ///// <returns></returns>
        //[DllImport("kernel32.dll")]
        //public static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        ///// <summary>
        ///// https://learn.microsoft.com/en-US/windows/win32/api/fileapi/nf-fileapi-createfilew
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <param name="access"></param>
        ///// <param name="share"></param>
        ///// <param name="securityAttributes"></param>
        ///// <param name="creationDisposition"></param>
        ///// <param name="flagsAndAttributes"></param>
        ///// <param name="templateFile"></param>
        ///// <returns></returns>
        //[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        //public static extern SafeFileHandle CreateFileW(
        //    [MarshalAs(UnmanagedType.LPWStr)] string filename,
        //    [MarshalAs(UnmanagedType.U4)] FileAccess access,
        //    [MarshalAs(UnmanagedType.U4)] FileShare share,
        //    IntPtr securityAttributes,
        //    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        //    [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        //    IntPtr templateFile);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shfileoperationw
        /// </summary>
        /// <param name="lpFileOp"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        public static extern int SHFileOperationW32(ref SHFILEOPSTRUCTW32 lpFileOp);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shfileoperationw
        /// </summary>
        /// <param name="lpFileOp"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        public static extern int SHFileOperationW64(ref SHFILEOPSTRUCTW64 lpFileOp);
    }
}
