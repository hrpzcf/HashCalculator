// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include "commons.h"
#include "ComputeHash.h"
#include "resource.h"
#include <atlcore.h>
#include <climits>
#include <map>
#include <shlobj_core.h>
#include <Shlwapi.h>
#include <ShObjIdl_core.h>
#include <shtypes.h>
#include <string>
#include <strsafe.h>
#include <utility>
#include <vector>
#include <wchar.h>
#include <Windows.h>


VOID CComputeHash::CreateGUIProcessComputeHash(const wstring& algos) {
    if (this->vFilepathList.empty()) {
        return;
    }
    wstring exe_path = GetHashCalculatorPath();
    if (exe_path.empty() || !PathFileExistsW(exe_path.c_str())) {
        ShowMessageType(this->hModule, IDS_TITLE_ERROR, IDS_NO_EXECUTABLE_PATH,
            MB_TOPMOST | MB_ICONERROR);
        return;
    }
    // 此处的字符 'p' 不是传给 HashCalculator 的命令，仅作为占位符，C# 程序接收不到此字符
    // 因为 C# 程序 Main 函数的 string[] args 参数仅从 CreateProcessW 第二个参数解析得来，
    // 且 C# Main 函数的 string[] args 参数为了不带可执行文件名，无条件删除解析得到的列表第一项，
    // 但在此函数的末尾，根本没有把可执行文件名和命令合并放在 CreateProcessW 第二个参数，
    // 就造成了 C# 程序错误把命令行参数的第一项（也就是此处的字符 'p'）当作可执行文件名给删掉
    wstring command_line_buffer = wstring(L"p compute");
    if (!algos.empty()) {
        command_line_buffer.append(L" --algo ");
        command_line_buffer.append(algos);
    }
    // 不算终止符的情况下，总长不得 >= CreateProcessW 的命令行上限 SHRT_MAX
    for (SIZE_T i = 0; i < this->vFilepathList.size(); ++i) {
        // 以 '\' 结尾的路径，直接用双引号包裹会让末尾的 '\' 紧贴结束引号，
        // 被 C# 端按 CommandLineToArgvW 规则误判为转义符而吞掉引号；补 '\' 成 '\\'，
        // 则被解析为一个字面反斜杠，引号正常闭合。文件路径不以 '\' 结尾，无需处理
        if (this->vFilepathList[i].back() == L'\\') {
            this->vFilepathList[i].push_back(L'\\');
        }
        // 该段路径为 ' ' + '"' + 路径 + '"'，长度即路径长 + 3
        SIZE_T item_length = this->vFilepathList[i].length() + 3;
        if (command_line_buffer.length() + item_length >= SHRT_MAX) {
            // 已经超过命令行参数字符数上限，丢弃该路径及后续路径
            break;
        }
        command_line_buffer.append(L" \"");
        command_line_buffer.append(this->vFilepathList[i]);
        command_line_buffer.push_back(L'"');
    }
    // CreateProcessW 要求 lpCommandLine 为可写的缓冲，且有可能写入包括终止符
    // 因为 C++ 11 保证有终止符但不可写，所以我们用 push_back(L'\0') 意在补一个可写空间
    // 注意：补了 '\0' 后不得再把 command_line_buffer 当成普通字符串拼接或求长
    command_line_buffer.push_back(L'\0');
    STARTUPINFOW startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessW(exe_path.c_str(), &command_line_buffer[0], NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
        NULL, NULL, &startup_info, &proc_info)) {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
}


CComputeHash::CComputeHash() {
    this->hModule = _AtlBaseModule.GetModuleInstance();
    this->hBitmapMenu = (HBITMAP)LoadImageW(
        this->hModule, MAKEINTRESOURCEW(IDB_BITMAP_MENU1), IMAGE_BITMAP, 0, 0,
        LR_DEFAULTSIZE | LR_SHARED);
    // 动态获取模块路径：返回值 >= 缓冲容量即表示缓冲不足、路径被截断，故扩容重试
    wstring module_path;
    DWORD capacity = MAX_PATH;
    DWORD path_length = 0;
    for (;;) {
        module_path.resize((SIZE_T)capacity);
        path_length = GetModuleFileNameW(this->hModule, &module_path[0], capacity);
        // 返回 0 表示失败；小于容量表示缓冲充足、路径完整
        if (0 == path_length || path_length < capacity) {
            break;
        }
        capacity += MAX_PATH;
    }
    // GetModuleFileNameW 保证 '\0' 结尾，PathRemoveFileSpecW 只认 '\0'，故不需先 resize
    if (0 != path_length && path_length < capacity && PathRemoveFileSpecW(&module_path[0])) {
        // PathRemoveFileSpecW 不更新 size，而 wstring 拼接按 size 非 '\0'，先 resize 同步
        module_path.resize(wcslen(module_path.c_str()));
        this->MenuJsonPath.append(module_path).push_back(L'\\');
        this->MenuJsonPath.append(MENU_JSONNAME);
    }
}


CComputeHash::~CComputeHash() {
}


STDMETHODIMP CComputeHash::Initialize(PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj,
    HKEY hkeyProgID) {

    this->vFilepathList.clear();
    this->mIsBackgroundContext = false;
    if (nullptr != pidlFolder) {
        // Directory\Background 上下文
        this->mIsBackgroundContext = true;
        // SHGetPathFromIDListW 固定按 MAX_PATH 写入，无法容纳长路径，
        // 故改用 SHGetPathFromIDListEx：它接受显式字符容量，可摆脱 260 上限。
        // 该 API 不提供「查询所需长度」的模式，故先给一个充裕的容量（MAX_PATH * 8 = 2080）
        // 写入成功后以已知容量 resize 收紧到实际长度。
        wstring directory_path;
        directory_path.resize((SIZE_T)(MAX_PATH * 8));
        if (SHGetPathFromIDListEx(pidlFolder, &directory_path[0],
            (DWORD)directory_path.size(), GPFIDL_DEFAULT)) {
            // 收紧到实际长度：wcslen 不含终止符，wstring 自行维护终止符，故 size 即实际字符数
            directory_path.resize(wcslen(directory_path.c_str()));
            this->vFilepathList.push_back(std::move(directory_path));
            return S_OK;
        }
        return E_INVALIDARG;
    }
    if (nullptr == pdtobj) return E_INVALIDARG;
    STGMEDIUM	stg = { TYMED_HGLOBAL };
    FORMATETC	fmt = {
        CF_HDROP,
        nullptr,
        DVASPECT_CONTENT,
        -1,
        TYMED_HGLOBAL };
    if (FAILED(pdtobj->GetData(&fmt, &stg))) {
        return E_INVALIDARG;
    }
    HDROP drop_handle = (HDROP)GlobalLock(stg.hGlobal);
    if (nullptr == drop_handle) {
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    // 传入 INFINITE 时 DragQueryFile 返回拖放的文件数量，A/W 版本结果一致
    UINT file_count = DragQueryFileW(drop_handle, INFINITE, nullptr, 0);
    if (0 == file_count) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    for (UINT index = 0; index < file_count; index++) {
        // 两段式取路径：先查所需字符数（不含终止符）
        UINT content_chars = DragQueryFileW(drop_handle, index, nullptr, 0);
        if (0 == content_chars) {
            continue;
        }
        wstring file_path;
        // 多分配 1 个字符用于容纳 DragQueryFile 写入的终止符，否则会越界写 1 个 wchar_t
        file_path.resize((SIZE_T)content_chars + 1);
        if (0 != DragQueryFileW(drop_handle, index, &file_path[0], content_chars + 1)) {
            // 收紧：wstring 自行维护终止符，不计入 size
            file_path.resize((SIZE_T)content_chars);
            this->vFilepathList.push_back(std::move(file_path));
        }
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}


STDMETHODIMP CComputeHash::QueryContextMenu(HMENU hMenu, UINT indexMenu, UINT idCmdFirst,
    UINT idCmdLast, UINT uFlags) {

    if (uFlags & CMF_DEFAULTONLY || this->MenuJsonPath.empty()) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    // 右击固定在快速访问的目录时会触发两次：Directory 和 Directory\Background，
    // 那我们选择此时不插入 Directory（即 m_isBackgroundContext == false）的菜单

    // QueryContextMenu uFlags
    // #define CMF_NORMAL              0x00000000
    // #define CMF_DEFAULTONLY         0x00000001
    // #define CMF_VERBSONLY           0x00000002
    // #define CMF_EXPLORE             0x00000004
    // #define CMF_NOVERBS             0x00000008
    // #define CMF_CANRENAME           0x00000010
    // #define CMF_NODEFAULT           0x00000020
    // #if (NTDDI_VERSION < NTDDI_VISTA)
    // #define CMF_INCLUDESTATIC       0x00000040
    // #endif
    // #if (NTDDI_VERSION >= NTDDI_VISTA)
    // #define CMF_ITEMMENU            0x00000080
    // #endif
    // #define CMF_EXTENDEDVERBS       0x00000100
    // #if (NTDDI_VERSION >= NTDDI_VISTA)
    // #define CMF_DISABLEDVERBS       0x00000200
    // #endif
    // #define CMF_ASYNCVERBSTATE      0x00000400
    // #define CMF_OPTIMIZEFORINVOKE   0x00000800
    // #define CMF_SYNCCASCADEMENU     0x00001000
    // #define CMF_DONOTPICKDEFAULT    0x00002000
    // #define CMF_RESERVED            0xffff0000

    // 通过检查 uFlags 来判断是否为快速访问右击
    // 右击快速访问：
    // uFlags==0x00000414: CMF_EXPLORE|CMF_CANRENAME|CMF_ASYNCVERBSTATE
    // 右击文件夹背景：
    // uFlags==0x00020424: CMF_EXPLORE|CMF_NODEFAULT|CMF_ASYNCVERBSTATE|CMF_RESERVED
    // 右击正常文件夹：
    // uFlags==0x00020494: CMF_EXPLORE|CMF_CANRENAME|CMF_ITEMMENU|CMF_ASYNCVERBSTATE|CMF_RESERVED

    // 有 3 个判断方法：
    // mIsBackgroundContext = false 且 uFlags 没有 CMF_ITEMMENU 标识 → 快速访问
    // mIsBackgroundContext = false 且 uFlags 没有 CMF_RESERVED 标识 → 快速访问
    // uFlags == 0x00000414 → 快速访问
    if (!this->mIsBackgroundContext && !(uFlags & CMF_ITEMMENU)) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    UINT id_cmd_current = 0;
    if (!InsertMenuFromJsonFile(this->MenuJsonPath, hMenu, indexMenu, idCmdFirst, idCmdLast,
        MENUTYPE_COMPUTE, &id_cmd_current, this->mIDCmdToAlgos, this->hBitmapMenu)) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, id_cmd_current);
}


STDMETHODIMP CComputeHash::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb)) {
        return E_INVALIDARG;
    }
    map<UINT, wstring>::iterator iter = this->mIDCmdToAlgos.find(LOWORD(pici->lpVerb));
    if (iter == this->mIDCmdToAlgos.end()) {
        return E_INVALIDARG;
    }
    this->CreateGUIProcessComputeHash(iter->second);
    return S_OK;
}


STDMETHODIMP CComputeHash::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax) {
    return E_NOTIMPL;
}
