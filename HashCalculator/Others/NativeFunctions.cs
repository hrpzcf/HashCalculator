using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHFILEOPSTRUCTW64
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
    internal struct SHFILEOPSTRUCTW32
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

    internal enum FileFuncFlags : uint
    {
        FO_MOVE = 0x1,
        FO_COPY = 0x2,
        FO_DELETE = 0x3,
        FO_RENAME = 0x4
    }

    [Flags]
    internal enum FILEOP_FLAGS : ushort
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
        /// Don't put up _errorCode UI
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

    internal enum ShowCmd : int
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
    internal enum SEMaskFlags : uint
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

    internal static class WM
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-changecbchain
        /// </summary>
        public const int WM_CHANGECBCHAIN = 0x030D;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-copydata
        /// </summary>
        public const int WM_COPYDATA = 0x004A;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-clipboardupdate
        /// </summary>
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-destroyclipboard
        /// </summary>
        public const int WM_DESTROYCLIPBOARD = 0x0307;
    }

    /// <summary>
    /// SetWindowPos 函数的参数
    /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
    /// </summary>
    public static class SWP
    {
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;
        public const uint SWP_DEFERERASE = 0x2000;
        public const uint SWP_DRAWFRAME = 0x0020;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_HIDEWINDOW = 0x0080;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_NOCOPYBITS = 0x0100;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOREPOSITION = 0x0200;
        public const uint SWP_NOSENDCHANGING = 0x0400;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    }

    /// <summary>
    /// ShowWindow 函数参数
    /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
    /// </summary>
    public static class SW
    {
        public const int SW_HIDE = 0; // 隐藏窗口并激活另一个窗口
        public const int SW_SHOWNORMAL = 1; // 激活并显示一个窗口,应用程序应在第一次显示窗口时指定此标志
        public const int SW_NORMAL = 1; // 激活并显示一个窗口,应用程序应在第一次显示窗口时指定此标志
        public const int SW_SHOWMINIMIZED = 2; // 激活窗口并将其显示为最小化窗口
        public const int SW_SHOWMAXIMIZED = 3; // 激活窗口并将其显示为最大化窗口
        public const int SW_MAXIMIZE = 3; // 激活窗口并将其显示为最大化窗口
        public const int SW_SHOWNOACTIVATE = 4; // 以最近的大小和位置显示窗口但不激活
        public const int SW_SHOW = 5; // 激活窗口并以其当前大小和位置显示它
        public const int SW_MINIMIZE = 6; // 最小化指定窗口并激活Z顺序中的下一个顶级窗口
        public const int SW_SHOWMINNOACTIVE = 7; // 将窗口显示为最小化窗口但不激活
        public const int SW_SHOWNA = 8; // 以当前大小和位置显示窗口但不激活
        public const int SW_RESTORE = 9; // 激活并显示窗口,应用程序在恢复最小化窗口时应指定此标志
        public const int SW_SHOWDEFAULT = 10; // 根据启动应用程序的程序传递给CreateProcess函数的STARTUPINFO结构中指定的SW_值设置
        public const int SW_FORCEMINIMIZE = 11; // 最小化一个窗口，即使拥有该窗口的线程没有响应
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptrw
    /// </summary>
    public static class GWL
    {
        public const int GWL_EXSTYLE = -20; // 检索 扩展窗口样式
        public const int GWLP_HINSTANCE = -6; // 检索应用程序实例的句柄
        public const int GWLP_HWNDPARENT = -8; // 检索父窗口的句柄（如果有）
        public const int GWLP_ID = -12; // 检索窗口的标识符
        public const int GWL_STYLE = -16; // 检索 窗口样式
        public const int GWLP_USERDATA = -21; // 检索与窗口关联的用户数据。此数据供创建窗口的应用程序使用。其值最初为零
        public const int GWLP_WNDPROC = -4; // 检索指向窗口过程的指针，或表示指向窗口过程的指针的句柄。必须使用CallWindowProc函数调用窗口过程
    }

    public static class WS
    {
        public const int WS_EX_TOPMOST = 0x00000008;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfow
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHELLEXECUTEINFOW
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
        public ShowCmd nShow;
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
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct BY_HANDLE_FILE_INFORMATION
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint dwVolumeSerialNumber;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint nNumberOfLinks;
        public uint nFileIndexHigh;
        public uint nFileIndexLow;
    }

    #region enum HChangeNotifyEventID
    /// <summary>
    /// Describes the event that has occurred.
    /// Typically, only one event is specified at a time.
    /// If more than one event is specified, the values contained
    /// in the <i>dwItem1</i> and <i>dwItem2</i>
    /// parameters must be the same, respectively, for all specified events.
    /// This parameter can be one or more of the following values.
    /// </summary>
    /// <remarks>
    /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
    /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
    /// </remarks>
    [Flags]
    internal enum HChangeNotifyEventID
    {
        /// <summary>
        /// Cover events have occurred.
        /// </summary>
        SHCNE_ALLEVENTS = 0x7FFFFFFF,

        /// <summary>
        /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// must be specified in the <i>uFlags</i> parameter.
        /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>.
        /// </summary>
        SHCNE_ASSOCCHANGED = 0x08000000,

        /// <summary>
        /// The attributes of an item or folder have changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item or folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_ATTRIBUTES = 0x00000800,

        /// <summary>
        /// A nonfolder item has been created.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_CREATE = 0x00000002,

        /// <summary>
        /// A nonfolder item has been deleted.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was deleted.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DELETE = 0x00000004,

        /// <summary>
        /// A drive has been added.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADD = 0x00000100,

        /// <summary>
        /// A drive has been added and the Shell should create a new window for the drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADDGUI = 0x00010000,

        /// <summary>
        /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEREMOVED = 0x00000080,

        /// <summary>
        /// Not currently used.
        /// </summary>
        SHCNE_EXTENDED_EVENT = 0x04000000,

        /// <summary>
        /// The amount of free space on a drive has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_FREESPACE = 0x00040000,

        /// <summary>
        /// Storage media has been inserted into a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that contains the new media.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAINSERTED = 0x00000020,

        /// <summary>
        /// Storage media has been removed from a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive from which the media was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAREMOVED = 0x00000040,

        /// <summary>
        /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MKDIR = 0x00000008,

        /// <summary>
        /// A folder on the local computer is being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETSHARE = 0x00000200,

        /// <summary>
        /// A folder on the local computer is no longer being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is no longer being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETUNSHARE = 0x00000400,

        /// <summary>
        /// The name of a folder has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder.
        /// <i>dwItem2</i> contains the new PIDL or name of the folder.
        /// </summary>
        SHCNE_RENAMEFOLDER = 0x00020000,

        /// <summary>
        /// The name of a nonfolder item has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous PIDL or name of the item.
        /// <i>dwItem2</i> contains the new PIDL or name of the item.
        /// </summary>
        SHCNE_RENAMEITEM = 0x00000001,

        /// <summary>
        /// A folder has been removed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_RMDIR = 0x00000010,

        /// <summary>
        /// The computer has disconnected from a server.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the server from which the computer was disconnected.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_SERVERDISCONNECT = 0x00004000,

        /// <summary>
        /// The contents of an existing folder have changed,
        /// but the folder still exists and has not been renamed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or
        /// SHCNE_RENAMEFOLDER, respectively, instead.
        /// </summary>
        SHCNE_UPDATEDIR = 0x00001000,

        /// <summary>
        /// An image in the system image list has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>.
        /// </summary>
        SHCNE_UPDATEIMAGE = 0x00008000,

    }
    #endregion // enum HChangeNotifyEventID

    #region public enum HChangeNotifyFlags
    /// <summary>
    /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters.
    /// The uFlags parameter must be one of the following values.
    /// </summary>
    [Flags]
    internal enum HChangeNotifyFlags
    {
        /// <summary>
        /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values.
        /// </summary>
        SHCNF_DWORD = 0x0003,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that
        /// represent the item(s) affected by the change.
        /// Each ITEMIDLIST must be relative to the desktop folder.
        /// </summary>
        SHCNF_IDLIST = 0x0000,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHA = 0x0001,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHW = 0x0005,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERA = 0x0002,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERW = 0x0006,
        /// <summary>
        /// The function should not return until the notification
        /// has been delivered to all affected components.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSH = 0x1000,
        /// <summary>
        /// The function should begin delivering notifications to all affected components
        /// but should return as soon as the notification process has begun.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSHNOWAIT = 0x2000
    }
    #endregion // enum HChangeNotifyFlags

    internal static class NativeFunctions
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-colorhlstorgb
        /// </summary>
        [DllImport("shlwapi.dll")]
        public static extern uint ColorHLSToRGB(int wHue, int wLuminance, int wSaturation);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern void SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecutew
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, ShowCmd nShowCmd);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool ShellExecuteExW(ref SHELLEXECUTEINFOW lpExecInfo);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/handleapi/nf-handleapi-closehandle
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
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setwindowpos
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
}
