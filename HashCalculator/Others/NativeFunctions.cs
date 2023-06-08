using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
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
    }
}
