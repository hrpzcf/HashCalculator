#include "pch.h"
#include "commons.h"
#include "OpenAsChecklist.h"
#include "resource.h"
#include <atlcore.h>
#include <climits>
#include <map>
#include <Shlwapi.h>
#include <ShObjIdl_core.h>
#include <shtypes.h>
#include <string>
#include <strsafe.h>
#include <wchar.h>
#include <Windows.h>


VOID COpenAsChecklist::CreateGUIProcessVerifyHash(const wstring& algos) const {
    if (this->mChecklistPath.empty()) {
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
    wstring command_line_buffer = wstring(L"p verify");
    if (!algos.empty()) {
        command_line_buffer.append(L" --algo ").append(algos);
    }
    command_line_buffer.append(L" --list");
    // 计算追加清单路径后的总字符数：路径前 1 空格 + 前后 2 引号
    // 不算终止符的情况下，不得 >= CreateProcessW 的命令行上限 SHRT_MAX
    if (command_line_buffer.length() + this->mChecklistPath.length() + 3 >= SHRT_MAX) {
        return;
    }
    command_line_buffer.append(L" \"");
    command_line_buffer.append(this->mChecklistPath);
    command_line_buffer.push_back(L'\"');
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


COpenAsChecklist::COpenAsChecklist() {
    this->hModule = _AtlBaseModule.GetModuleInstance();
    this->hBitmapMenu = (HBITMAP)LoadImageW(
        this->hModule, MAKEINTRESOURCEW(IDB_BITMAP_MENU3), IMAGE_BITMAP, 0, 0,
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


COpenAsChecklist::~COpenAsChecklist() {
}


STDMETHODIMP COpenAsChecklist::Initialize(PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj,
    HKEY hkeyProgID) {

    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    this->mChecklistPath.clear();
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
    // 传入 INFINITE 时 DragQueryFile 返回拖放的文件数量，此分支不涉及字符串，A/W 版本结果一致
    if (1 != DragQueryFileW(drop_handle, INFINITE, nullptr, 0)) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    // 两段式取路径：先查所需字符数（不含终止符），据此分配，故无 MAX_PATH 上限
    UINT content_chars = DragQueryFileW(drop_handle, 0, nullptr, 0);
    if (0 == content_chars) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    // 多分配 1 个字符用于容纳 DragQueryFile 写入的终止符，否则会越界写 1 个 wchar_t
    this->mChecklistPath.resize((SIZE_T)content_chars + 1);
    if (0 == DragQueryFileW(drop_handle, 0, &this->mChecklistPath[0], content_chars + 1)) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        this->mChecklistPath.clear();
        return E_FAIL;
    }
    // 收紧：wstring 自行维护终止符，不计入 size()
    this->mChecklistPath.resize((SIZE_T)content_chars);
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}


STDMETHODIMP COpenAsChecklist::QueryContextMenu(HMENU hMenu, UINT indexMenu, UINT idCmdFirst,
    UINT idCmdLast, UINT uFlags) {

    if (uFlags & CMF_DEFAULTONLY) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    UINT id_cmd_current = 0;
    if (!InsertMenuFromJsonFile(this->MenuJsonPath, hMenu, indexMenu, idCmdFirst, idCmdLast,
        MENUTYPE_CHECKHASH, &id_cmd_current, this->mIDCmdToAlgos, this->hBitmapMenu)) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, id_cmd_current);
}


STDMETHODIMP COpenAsChecklist::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb)) {
        return E_INVALIDARG;
    }
    map<UINT, wstring>::iterator iter = this->mIDCmdToAlgos.find(LOWORD(pici->lpVerb));
    if (iter == this->mIDCmdToAlgos.end()) {
        return E_INVALIDARG;
    }
    this->CreateGUIProcessVerifyHash(iter->second);
    return S_OK;
}


STDMETHODIMP COpenAsChecklist::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax) {
    return E_NOTIMPL;
}
