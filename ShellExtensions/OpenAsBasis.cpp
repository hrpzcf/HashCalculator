// OpenAsBasis.cpp: COpenAsBasis 的实现

#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "OpenAsBasis.h"
#include "ResString.h"

constexpr auto IDM_VERIFY_AUTO = 0;
constexpr auto IDM_VERIFY_XXHASH32 = 1;
constexpr auto IDM_VERIFY_XXHASH64 = 2;
constexpr auto IDM_VERIFY_XXHASH3 = 3;
constexpr auto IDM_VERIFY_XXHASH128 = 4;
constexpr auto IDM_VERIFY_MD5 = 5;
constexpr auto IDM_VERIFY_CRC32 = 6;
constexpr auto IDM_VERIFY_CRC64 = 7;
constexpr auto IDM_VERIFY_QUICKXOR = 8;
constexpr auto IDM_VERIFY_WHIRLPOOL = 9;
constexpr auto IDM_VERIFY_SHA1 = 10;
constexpr auto IDM_VERIFY_SHA224 = 11;
constexpr auto IDM_VERIFY_SHA256 = 12;
constexpr auto IDM_VERIFY_SHA384 = 13;
constexpr auto IDM_VERIFY_SHA512 = 14;
constexpr auto IDM_VERIFY_SHA3_224 = 15;
constexpr auto IDM_VERIFY_SHA3_256 = 16;
constexpr auto IDM_VERIFY_SHA3_384 = 17;
constexpr auto IDM_VERIFY_SHA3_512 = 18;
constexpr auto IDM_VERIFY_BLAKE2B = 19;
constexpr auto IDM_VERIFY_BLAKE2BP = 20;
constexpr auto IDM_VERIFY_BLAKE2S = 21;
constexpr auto IDM_VERIFY_BLAKE2SP = 22;
constexpr auto IDM_VERIFY_BLAKE3 = 23;
constexpr auto IDM_VERIFY_STREEBOG256 = 24;
constexpr auto IDM_VERIFY_PARENT = 25;

VOID COpenAsBasis::CreateGUIProcessVerifyHash(LPWSTR algo) {
    if (nullptr == this->basis_path) {
        return;
    }
    if (nullptr == this->executable_path) {
        ResWString title = ResWString(this->module_inst, IDS_TITLE_ERROR);
        ResWString text = ResWString(this->module_inst, IDS_NO_EXECUTABLE_PATH);
        MessageBoxW(nullptr, text.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
        return;
    }
    // 此处的字符 'p' 不是传给 HashCalculator 的命令，仅作为占位符，C# 程序接收不到此字符。
    // 因为 C# 程序 Main 函数的 string[] args 参数仅从 CreateProcessW 第二个参数解析得来，
    // 且 C# Main 函数的 string[] args 参数为了不带可执行文件名，CLR 又无脑地删除了解析得到的列表的第一项，
    // 它以为第一项一定是可执行文件名，但在此函数末尾作者根本没有把可执行文件名和命令合并放在 CreateProcessW 第二个参数，
    // 就造成了 C# CLR 错误地把命令行参数的第一项（也就是此处的字符 'p'）当作可执行文件名给删了。
    wstring command_line = wstring(L"p verify");
    if (nullptr != algo) {
        command_line += wstring(L" --algo ") + algo;
    }
    command_line += L" --basis";
    // 为什么 +4：basis_path 前面 1 个空格和前后 2 个引号，1 个终止字符
    SIZE_T cmd_characters = command_line.length() + wcslen(this->basis_path) + 4;
    if (cmd_characters > MAX_CMD_CHARS) {
        return;
    }
    command_line += L" \"" + wstring(this->basis_path) + L"\"";
    LPWSTR commandline_buffer;
    try {
        commandline_buffer = new WCHAR[cmd_characters];
    }
    catch (const std::bad_alloc&) {
        return;
    }
    StringCchCopyW(commandline_buffer, cmd_characters, command_line.c_str());
    STARTUPINFO startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessW(this->executable_path, commandline_buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
        NULL, NULL, &startup_info, &proc_info))
    {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
    delete[] commandline_buffer;
}

COpenAsBasis::COpenAsBasis() {
    this->module_inst = _AtlBaseModule.GetModuleInstance();
    try
    {
        DWORD bufsize = MAX_PATH;
        LPWSTR  module_dirpath = new WCHAR[bufsize];
        while (true)
        {
            DWORD size = GetModuleFileNameW(module_inst, module_dirpath, bufsize);
            if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
                delete[]  module_dirpath;
                bufsize += MAX_PATH;
                module_dirpath = new WCHAR[bufsize];
                continue;
            }
            if (!PathRemoveFileSpecW(module_dirpath)) {
                break;
            }
            SIZE_T moduledir_chars = wcslen(module_dirpath);
            if (moduledir_chars == 0) {
                break;
            }
            SIZE_T exec_total_chars = moduledir_chars + 2 + wcslen(EXECUTABLE);
            this->executable_path = new WCHAR[exec_total_chars]();
            StringCchCatW(this->executable_path, exec_total_chars, module_dirpath);
            StringCchCatW(this->executable_path, exec_total_chars, L"\\");
            StringCchCatW(this->executable_path, exec_total_chars, EXECUTABLE);
            break;
        }
        delete[] module_dirpath;
    }
    catch (const std::bad_alloc&) {
    }
    this->bitmap_menu = LoadBitmapW(module_inst, MAKEINTRESOURCEW(IDB_BITMAP_MENU3));
}

COpenAsBasis::~COpenAsBasis() {
    delete[] this->executable_path;
    delete[] this->basis_path;
    DeleteObject(this->bitmap_menu);
}

STDMETHODIMP COpenAsBasis::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    delete[] this->basis_path;
    this->basis_path = nullptr;
    STGMEDIUM	stg = { TYMED_HGLOBAL };
    FORMATETC	fmt = {
        CF_HDROP,
        nullptr,
        DVASPECT_CONTENT,
        -1,
        TYMED_HGLOBAL };
    HDROP		drop_handle = nullptr;
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
    if (1 != DragQueryFileW(drop_handle, INFINITE, nullptr, 0))
    {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    UINT chars = DragQueryFileW(drop_handle, 0, nullptr, 0) + 1;
    if (0 == chars) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    try {
        this->basis_path = new WCHAR[chars];
    }
    catch (const std::bad_alloc&) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_FAIL;
    }
    if (0 == DragQueryFileW(drop_handle, 0, this->basis_path, chars)) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        delete[] this->basis_path;
        this->basis_path = nullptr;
        return E_FAIL;
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}

STDMETHODIMP COpenAsBasis::QueryContextMenu(
    HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    HMENU hParentMenu = CreatePopupMenu();
    LONG flag = MF_STRING | MF_POPUP;
    ResWString autoAlgoRes = ResWString(this->module_inst, IDS_MENU_VERIFY_AUTO);
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_AUTO, autoAlgoRes.String());
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_XXHASH32, L"XXH32");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_XXHASH64, L"XXH64");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_XXHASH3, L"XXH3");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_XXHASH128, L"XXH128");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_MD5, L"MD5");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_CRC32, L"CRC32");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_CRC64, L"CRC64");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_QUICKXOR, L"QuickXor");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_WHIRLPOOL, L"Whirlpool");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA1, L"SHA-1");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA224, L"SHA-224");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA256, L"SHA-256");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA384, L"SHA-384");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA512, L"SHA-512");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA3_224, L"SHA3-224");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA3_256, L"SHA3-256");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA3_384, L"SHA3-384");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_SHA3_512, L"SHA3-512");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_BLAKE2B, L"BLAKE2b-512");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_BLAKE2BP, L"BLAKE2bp-512");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_BLAKE2S, L"BLAKE2s-256");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_BLAKE2SP, L"BLAKE2sp-256");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_BLAKE3, L"BLAKE3-256");
    AppendMenuW(hParentMenu, flag, idCmdFirst + IDM_VERIFY_STREEBOG256, L"Streebog-256");
    // 方法退出后 parentMenuRes 会被析构，parentMenuText 会被 delete
    // menuInfo.dwTypeData = parentMenuText 安全? InsertMenuItemW 是否复制数据?
    ResWString parentMenuRes = ResWString(this->module_inst, IDS_MENU_VERIFY);
    LPWSTR parentMenuText = parentMenuRes.String();
    MENUITEMINFOW menuInfo = { 0 };
    menuInfo.cbSize = sizeof(MENUITEMINFOW);
    menuInfo.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_TYPE;
    menuInfo.fType = MFT_STRING;
    menuInfo.wID = idCmdFirst + IDM_VERIFY_PARENT;
    menuInfo.hSubMenu = hParentMenu;
    menuInfo.dwTypeData = parentMenuText;
    menuInfo.cch = (UINT)wcslen(parentMenuText);
    if (nullptr != this->bitmap_menu) {
        menuInfo.fMask |= MIIM_CHECKMARKS;
        menuInfo.hbmpChecked = this->bitmap_menu;
        menuInfo.hbmpUnchecked = this->bitmap_menu;
    }
    InsertMenuItemW(hmenu, indexMenu + 1, true, &menuInfo);
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, IDM_VERIFY_PARENT + 1);
}

STDMETHODIMP COpenAsBasis::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    LPWSTR algo = nullptr;
    switch (LOWORD(pici->lpVerb))
    {
    case IDM_VERIFY_AUTO:
        break;
    case IDM_VERIFY_XXHASH32:
        algo = L"XXHASH32";
        break;
    case IDM_VERIFY_XXHASH64:
        algo = L"XXHASH64";
        break;
    case IDM_VERIFY_XXHASH3:
        algo = L"XXHASH3";
        break;
    case IDM_VERIFY_XXHASH128:
        algo = L"XXHASH128";
        break;
    case IDM_VERIFY_MD5:
        algo = L"MD5";
        break;
    case IDM_VERIFY_CRC32:
        algo = L"CRC32";
        break;
    case IDM_VERIFY_CRC64:
        algo = L"CRC64";
        break;
    case IDM_VERIFY_QUICKXOR:
        algo = L"QUICKXOR";
        break;
    case IDM_VERIFY_WHIRLPOOL:
        algo = L"WHIRLPOOL";
        break;
    case IDM_VERIFY_SHA1:
        algo = L"SHA1";
        break;
    case IDM_VERIFY_SHA224:
        algo = L"SHA224";
        break;
    case IDM_VERIFY_SHA256:
        algo = L"SHA256";
        break;
    case IDM_VERIFY_SHA384:
        algo = L"SHA384";
        break;
    case IDM_VERIFY_SHA512:
        algo = L"SHA512";
        break;
    case IDM_VERIFY_SHA3_224:
        algo = L"SHA3_224";
        break;
    case IDM_VERIFY_SHA3_256:
        algo = L"SHA3_256";
        break;
    case IDM_VERIFY_SHA3_384:
        algo = L"SHA3_384";
        break;
    case IDM_VERIFY_SHA3_512:
        algo = L"SHA3_512";
        break;
    case IDM_VERIFY_BLAKE2B:
        algo = L"BLAKE2B_512";
        break;
    case IDM_VERIFY_BLAKE2BP:
        algo = L"BLAKE2BP_512";
        break;
    case IDM_VERIFY_BLAKE2S:
        algo = L"BLAKE2S_256";
        break;
    case IDM_VERIFY_BLAKE2SP:
        algo = L"BLAKE2SP_256";
        break;
    case IDM_VERIFY_BLAKE3:
        algo = L"BLAKE3_256";
        break;
    case IDM_VERIFY_STREEBOG256:
        algo = L"STREEBOG_256";
        break;
    default:
        return E_INVALIDARG;
    }
    this->CreateGUIProcessVerifyHash(algo);
    return S_OK;
}

STDMETHODIMP COpenAsBasis::GetCommandString(
    UINT_PTR idCmd,
    UINT uType,
    UINT* pReserved,
    _Out_writes_bytes_((uType& GCS_UNICODE) ? (cchMax * sizeof(wchar_t)) : cchMax) _When_(!(uType& (GCS_VALIDATEA | GCS_VALIDATEW)), _Null_terminated_) CHAR* pszName,
    UINT cchMax) {
    return E_NOTIMPL;
}
