using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal static class USER32
    {
        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getclientrect
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowrect
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-clienttoscreen
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-screentoclient
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-closeclipboard
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-emptyclipboard
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboarddata
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardData(CF uFormat);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-isclipboardformatavailable
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool IsClipboardFormatAvailable(CF uFormat);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-openclipboard
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setclipboarddata
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardData(CF uFormat, IntPtr hMem);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-isiconic
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iszoomed
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setactivewindow
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr SetActiveWindow(IntPtr hWnd);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptrw
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int GetWindowLongPtrW(IntPtr hWnd, int nIndex);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messageboxw
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendmessagew
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int SendMessageW(IntPtr hWnd, uint msg, int wParam, int lParam);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setclipboardviewer
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-changeclipboardchain
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardowner
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardOwner();

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getopenclipboardwindow
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetOpenClipboardWindow();

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool AddClipboardFormatListener(IntPtr handle);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-removeclipboardformatlistener
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool RemoveClipboardFormatListener(IntPtr handle);
    }

    internal static class SHLWAPI
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-colorhlstorgb
        /// </summary>
        [DllImport("shlwapi.dll")]
        public static extern uint ColorHLSToRGB(int wHue, int wLuminance, int wSaturation);
    }

    internal static class SHELL32
    {
        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern void SHParseDisplayName(
            string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecutew
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr ShellExecuteW(
            IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, ShowCmd nShowCmd);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shfileoperationw
        /// </summary>
        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        internal static extern int SHFileOperationW32(ref SHFILEOPSTRUCTW32 lpFileOp);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shfileoperationw
        /// </summary>
        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        internal static extern int SHFileOperationW64(ref SHFILEOPSTRUCTW64 lpFileOp);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shchangenotify
        /// </summary>
        [DllImport("shell32.dll")]
        internal static extern void SHChangeNotify(HChangeNotifyEventID wEventId, HChangeNotifyFlags uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }

    internal static class KERNEL32
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// https://learn.microsoft.com/en-US/windows/win32/api/fileapi/nf-fileapi-createfilew
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// https://learn.microsoft.com/en-US/windows/win32/api/fileapi/nf-fileapi-createfilew
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setendoffile
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern bool SetEndOfFile(IntPtr hFile);
    }
}
