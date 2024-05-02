using System;

namespace HashCalculator
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-getdevicecaps
    /// </summary>
    internal enum DeviceCap
    {
        /// <summary>
        /// Horizontal width in pixels
        /// </summary>
        HORZRES = 8,

        /// <summary>
        /// Vertical height in pixels
        /// </summary>
        VERTRES = 10,

        /// <summary>
        /// Vertical height of entire desktop in pixels
        /// </summary>
        DESKTOPVERTRES = 117,

        /// <summary>
        /// Horizontal width of entire desktop in pixels
        /// </summary>
        DESKTOPHORZRES = 118,
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

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats
    /// </summary>
    internal enum CF : uint
    {
        CF_TEXT = 1,
        CF_BITMAP = 2,
        CF_METAFILEPICT = 3,
        CF_SYLK = 4,
        CF_DIF = 5,
        CF_TIFF = 6,
        CF_OEMTEXT = 7,
        CF_DIB = 8,
        CF_PALETTE = 9,
        CF_PENDATA = 10,
        CF_RIFF = 11,
        CF_WAVE = 12,
        CF_UNICODETEXT = 13,
        CF_ENHMETAFILE = 14,
        CF_HDROP = 15,
        CF_LOCALE = 16,
        CF_DIBV5 = 17,
        CF_OWNERDISPLAY = 0x0080,
        CF_DSPTEXT = 0x0081,
        CF_DSPBITMAP = 0x0082,
        CF_DSPMETAFILEPICT = 0x0083,
        CF_DSPENHMETAFILE = 0x008E,
        CF_PRIVATEFIRST = 0x0200,
        CF_PRIVATELAST = 0x02FF,
        CF_GDIOBJFIRST = 0x0300,
        CF_GDIOBJLAST = 0x03FF,
    }

    internal static class WM
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-clear
        /// </summary>
        public const int WM_CLEAR = 0x0303;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-copy
        /// </summary>
        public const int WM_COPY = 0x0301;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-cut
        /// </summary>
        public const int WM_CUT = 0x0300;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/dataxchg/wm-paste
        /// </summary>
        public const int WM_PASTE = 0x0302;

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

    /// <summary>
    /// https://learn.microsoft.com/zh-cn/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow
    /// </summary>
    [Flags]
    internal enum SHGFI : uint
    {
        /// <summary>
        /// 版本 5.0。 将相应的覆盖应用于文件的图标。 还必须设置 SHGFI_ICON 标志。
        /// </summary>
        SHGFI_ADDOVERLAYS = 0x000000020,
        /// <summary>
        /// 修改SHGFI_ATTRIBUTES以指示 psfi 上的 SHFILEINFO 结构的 dwAttributes 成员包含所需的特定属性。 这些属性将传递给 IShellFolder：：GetAttributesOf。 如果未指定此标志，0xFFFFFFFF将传递给 IShellFolder：：GetAttributesOf，请求所有属性。 不能使用 SHGFI_ICON 标志指定此标志。
        /// </summary>
        SHGFI_ATTR_SPECIFIED = 0x000020000,
        /// <summary>
        /// 检索项属性。 属性将复制到 psfi 参数中指定的结构的 dwAttributes 成员。 这些属性与从 IShellFolder：：GetAttributesOf 获取的属性相同。
        /// </summary>
        SHGFI_ATTRIBUTES = 0x000000800,
        /// <summary>
        /// 检索文件的显示名称，即 Windows 资源管理器中显示的名称。 该名称将复制到 psfi 中指定的结构的 szDisplayName 成员。 返回的显示名称使用长文件名（如果有），而不是文件名的 8.3 格式。 请注意，显示名称可能会受到设置的影响，例如是否显示扩展。
        /// </summary>
        SHGFI_DISPLAYNAME = 0x000000200,
        /// <summary>
        /// 如果 pszPath 标识了可执行文件，则检索可执行文件的类型。 信息将打包到返回值中。 此标志不能与任何其他标志一起指定。
        /// </summary>
        SHGFI_EXETYPE = 0x000002000,
        /// <summary>
        /// 检索表示文件图标的句柄，以及系统映像列表中图标的索引。 句柄将复制到 psfi 指定的结构的 hIcon 成员，并将索引复制到 iIcon 成员。
        /// </summary>
        SHGFI_ICON = 0x000000100,
        /// <summary>
        /// 检索包含表示 pszPath 指定的文件的图标的文件的名称，该文件的图标处理程序的 IExtractIcon：：GetIconLocation 方法返回。 此外，检索该文件中的图标索引。 包含图标的文件的名称将复制到 psfi 指定的结构的 szDisplayName 成员。 图标的索引将复制到该结构的 iIcon 成员。
        /// </summary>
        SHGFI_ICONLOCATION = 0x000001000,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数检索文件的大型图标。 还必须设置 SHGFI_ICON 标志。
        /// </summary>
        SHGFI_LARGEICON = 0x000000000,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数将链接覆盖添加到文件的图标。 还必须设置 SHGFI_ICON 标志。
        /// </summary>
        SHGFI_LINKOVERLAY = 0x000008000,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数检索文件的打开图标。 还用于修改 SHGFI_SYSICONINDEX，使函数返回包含文件打开小图标的系统映像列表的句柄。 容器对象显示一个打开图标，指示容器处于打开状态。 还必须设置 SHGFI_ICON 和/或 SHGFI_SYSICONINDEX 标志。
        /// </summary>
        SHGFI_OPENICON = 0x000000002,
        /// <summary>
        /// 版本 5.0。 返回覆盖图标的索引。 覆盖索引的值在 psfi 指定的结构的 iIcon 成员的八位中返回。 此标志还要求设置 SHGFI_ICON 。
        /// </summary>
        SHGFI_OVERLAYINDEX = 0x000000040,
        /// <summary>
        /// 指示 pszPath 是 ITEMIDLIST 结构的地址，而不是路径名称。
        /// </summary>
        SHGFI_PIDL = 0x000000008,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数将文件的图标与系统突出显示颜色混合。 还必须设置 SHGFI_ICON 标志。
        /// </summary>
        SHGFI_SELECTED = 0x000010000,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数检索 Shell 大小的图标。 如果未指定此标志，函数将根据系统指标值调整图标大小。 还必须设置 SHGFI_ICON 标志。
        /// </summary>
        SHGFI_SHELLICONSIZE = 0x000000004,
        /// <summary>
        /// 修改 SHGFI_ICON，使函数检索文件的小图标。 还用于修改 SHGFI_SYSICONINDEX，使 函数返回包含小图标图像的系统图像列表的句柄。 还必须设置 SHGFI_ICON 和/或 SHGFI_SYSICONINDEX 标志。
        /// </summary>
        SHGFI_SMALLICON = 0x000000001,
        /// <summary>
        /// 检索系统映像列表图标的索引。 如果成功，索引将复制到 psfi 的 iIcon 成员。 返回值是系统映像列表的句柄。 只有索引成功复制到 iIcon 的那些图像才有效。 尝试访问系统映像列表中的其他映像将导致未定义的行为。
        /// </summary>
        SHGFI_SYSICONINDEX = 0x000004000,
        /// <summary>
        /// 检索描述文件类型的字符串。 字符串将复制到 psfi 中指定的结构的 szTypeName 成员。
        /// </summary>
        SHGFI_TYPENAME = 0x000000400,
        /// <summary>
        /// 指示函数不应尝试访问 pszPath 指定的文件。 相反，它的行为应与 pszPath 指定的文件存在一样，其中包含在 dwFileAttributes 中传递的文件属性。 此标志不能与 SHGFI_ATTRIBUTES、 SHGFI_EXETYPE或 SHGFI_PIDL 标志组合使用。
        /// </summary>
        SHGFI_USEFILEATTRIBUTES = 0x000000010,
    }
}
