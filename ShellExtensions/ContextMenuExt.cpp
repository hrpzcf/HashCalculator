// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include "ContextMenuExt.h"
#include <strsafe.h>
#include <shlobj_core.h>
#include "ResourceStr.h"

const wstring exec_name(L"HashCalculator.exe");
constexpr auto IDM_COMPUTE_HASH = 0;
constexpr auto IDM_COMPUTE_SHA1 = 1;
constexpr auto IDM_COMPUTE_SHA224 = 2;
constexpr auto IDM_COMPUTE_SHA256 = 3;
constexpr auto IDM_COMPUTE_SHA384 = 4;
constexpr auto IDM_COMPUTE_SHA512 = 5;
constexpr auto IDM_COMPUTE_SHA3_224 = 6;
constexpr auto IDM_COMPUTE_SHA3_256 = 7;
constexpr auto IDM_COMPUTE_SHA3_384 = 8;
constexpr auto IDM_COMPUTE_SHA3_512 = 9;
constexpr auto IDM_COMPUTE_MD5 = 10;
constexpr auto IDM_COMPUTE_BLAKE2S = 11;
constexpr auto IDM_COMPUTE_BLAKE2B = 12;
constexpr auto IDM_COMPUTE_BLAKE3 = 13;
constexpr auto IDM_COMPUTE_WHIRLPOOL = 14;
constexpr auto IDM_SUBMENUS_PARENT = 15;

VOID CContextMenuExt::CreateGUIProcessComputeHash(LPCWSTR algo) {
    if (nullptr == this->module_dirpath) {
        CResStrW title = CResStrW(this->module_inst, IDS_TITLE_ERROR);
        CResStrW text = CResStrW(this->module_inst, IDS_NO_MODULE_DIRPATH);
        MessageBoxW(nullptr, text.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
        return;
    }
    wstring executable_path(wstring(this->module_dirpath) + L"\\" + exec_name);
    wstring arguments(L"compute");
    if (nullptr != algo) {
        arguments += wstring(L" --algo ") + algo;
    }
    for (SIZE_T i = 0; i < this->filepath_list.size(); ++i) {
        if (this->filepath_list[i][this->filepath_list[i].size() - 1] == L'\\') {
            this->filepath_list[i] += L'\\';
        }
        arguments += L" \"" + this->filepath_list[i] + L"\"";
    }
    SIZE_T args_wchar_count = arguments.size() + 1;
    LPWSTR arguments_buffer = new WCHAR[args_wchar_count];
    if (nullptr == arguments_buffer) {
        return;
    }
    StringCchCopyW(arguments_buffer, args_wchar_count, arguments.c_str());
    STARTUPINFO startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessW(executable_path.c_str(), arguments_buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
        NULL, NULL, &startup_info, &proc_info))
    {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
    delete[] arguments_buffer;
}

CContextMenuExt::CContextMenuExt() {
    this->module_inst = _AtlBaseModule.GetModuleInstance();
    try
    {
        DWORD bufsize = MAX_PATH;
        this->module_dirpath = new WCHAR[bufsize];
        while (true)
        {
            DWORD size = GetModuleFileNameW(module_inst, this->module_dirpath, bufsize);
            if (size == bufsize || GetLastError() & ERROR_INSUFFICIENT_BUFFER)
            {
                delete[] this->module_dirpath;
                this->module_dirpath = nullptr;
                bufsize += MAX_PATH;
                this->module_dirpath = new WCHAR[bufsize];
                continue;
            }
            PathRemoveFileSpecW(this->module_dirpath);
            break;
        }
    }
    catch (const std::bad_alloc&) {}
    this->bitmap_menu1 = LoadBitmapW(module_inst, MAKEINTRESOURCEW(IDB_BITMAP_MENU1));
    this->bitmap_menu2 = LoadBitmapW(module_inst, MAKEINTRESOURCEW(IDB_BITMAP_MENU2));
}

CContextMenuExt::~CContextMenuExt() {
    delete[] this->module_dirpath;
    DeleteObject(this->bitmap_menu1);
    DeleteObject(this->bitmap_menu2);
}

STDMETHODIMP CContextMenuExt::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    WCHAR filepath_buffer[MAX_PATH];
    this->filepath_list.clear();
    if (nullptr != pidlFolder) {
        if (SHGetPathFromIDListW(pidlFolder, filepath_buffer)) {
            this->filepath_list.push_back(filepath_buffer);
            return S_OK;
        }
        return E_INVALIDARG;
    }
    STGMEDIUM	stg = { TYMED_HGLOBAL };
    FORMATETC	fmt = {
        CF_HDROP,
        nullptr,
        DVASPECT_CONTENT,
        -1,
        TYMED_HGLOBAL };
    HDROP		drop_handle = nullptr;
    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    if (FAILED(pdtobj->GetData(&fmt, &stg)))
    {
        return E_INVALIDARG;
    }
    drop_handle = (HDROP)GlobalLock(stg.hGlobal);
    if (nullptr == drop_handle)
    {
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    UINT file_count = DragQueryFileW(drop_handle, INFINITE, nullptr, 0);
    if (0 == file_count)
    {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    for (UINT index = 0; index < file_count; index++)
    {
        if (0 != DragQueryFileW(drop_handle, index, filepath_buffer, MAX_PATH))
        {
            this->filepath_list.push_back(filepath_buffer);
        }
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}
STDMETHODIMP CContextMenuExt::QueryContextMenu(
    HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, USHORT(0));
    }
    CResStrW resource = CResStrW(this->module_inst, IDS_COMPUTE_MENU);
    InsertMenuW(hmenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP,
        idCmdFirst + IDM_COMPUTE_HASH, resource.String());
    if (this->bitmap_menu1 != nullptr) {
        SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, this->bitmap_menu1, this->bitmap_menu1);
    }
    HMENU submenu_handle = CreatePopupMenu();
    LONG flag = MF_STRING | MF_POPUP;
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA1, L"SHA1");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA224, L"SHA224");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA256, L"SHA256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA384, L"SHA384");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA512, L"SHA512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_224, L"SHA3-224");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_256, L"SHA3-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_384, L"SHA3-384");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_512, L"SHA3-512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_MD5, L"MD5");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2S, L"BLAKE2s");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2B, L"BLAKE2b");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE3, L"BLAKE3");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_WHIRLPOOL, L"Whirlpool");
    // 方法退出后 compute_hash_res 会被析构，compute_hash_text 会被 delete
    // menu_info.dwTypeData = compute_hash_text 是否安全？InsertMenuItemW 是否复制数据？
    CResStrW compute_hash_res = CResStrW(this->module_inst, IDS_COMPUTE_HASH_MENU);
    LPWSTR compute_hash_text = compute_hash_res.String();
    MENUITEMINFOW menu_info = { 0 };
    menu_info.cbSize = sizeof(MENUITEMINFOW);
    menu_info.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_TYPE;
    menu_info.fType = MFT_STRING;
    menu_info.wID = idCmdFirst + IDM_SUBMENUS_PARENT;
    menu_info.hSubMenu = submenu_handle;
    menu_info.dwTypeData = compute_hash_text;
    menu_info.cch = (UINT)wcslen(compute_hash_text);
    if (nullptr != this->bitmap_menu2) {
        menu_info.fMask |= MIIM_CHECKMARKS;
        menu_info.hbmpChecked = this->bitmap_menu2;
        menu_info.hbmpUnchecked = this->bitmap_menu2;
    }
    InsertMenuItemW(hmenu, indexMenu + 1, true, &menu_info);
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, USHORT(IDM_SUBMENUS_PARENT + 1));
}

STDMETHODIMP CContextMenuExt::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    LPCWSTR algo = nullptr;
    switch (LOWORD(pici->lpVerb))
    {
    case IDM_COMPUTE_HASH:
        break;
    case IDM_COMPUTE_SHA1:
        algo = L"SHA1";
        break;
    case IDM_COMPUTE_SHA224:
        algo = L"SHA224";
        break;
    case IDM_COMPUTE_SHA256:
        algo = L"SHA256";
        break;
    case IDM_COMPUTE_SHA384:
        algo = L"SHA384";
        break;
    case IDM_COMPUTE_SHA512:
        algo = L"SHA512";
        break;
    case IDM_COMPUTE_SHA3_224:
        algo = L"SHA3_224";
        break;
    case IDM_COMPUTE_SHA3_256:
        algo = L"SHA3_256";
        break;
    case IDM_COMPUTE_SHA3_384:
        algo = L"SHA3_384";
        break;
    case IDM_COMPUTE_SHA3_512:
        algo = L"SHA3_512";
        break;
    case IDM_COMPUTE_MD5:
        algo = L"MD5";
        break;
    case IDM_COMPUTE_BLAKE2S:
        algo = L"BLAKE2S";
        break;
    case IDM_COMPUTE_BLAKE2B:
        algo = L"BLAKE2B";
        break;
    case IDM_COMPUTE_BLAKE3:
        algo = L"BLAKE3";
        break;
    case IDM_COMPUTE_WHIRLPOOL:
        algo = L"WHIRLPOOL";
        break;
    default:
        return E_INVALIDARG;
    }
    CreateGUIProcessComputeHash(algo);
    return S_OK;
}

STDMETHODIMP CContextMenuExt::GetCommandString(
    UINT_PTR idCmd,
    UINT uType,
    UINT* pReserved,
    _Out_writes_bytes_((uType& GCS_UNICODE) ? (cchMax * sizeof(wchar_t)) : cchMax) _When_(!(uType& (GCS_VALIDATEA | GCS_VALIDATEW)), _Null_terminated_) CHAR* pszName,
    UINT cchMax) {
    return E_NOTIMPL;
}
