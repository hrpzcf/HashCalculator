// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "ComputeHash.h"
#include "ResString.h"


VOID CComputeHash::CreateGUIProcessComputeHash(LPCWSTR algo) {
    if (this->vFilepathList.empty()) {
        return;
    }
    DWORD bufferSize = MAX_PATH;
    LPWSTR exePathBuffer = new WCHAR[bufferSize]();
    if (!GetHashCalculatorPath(&exePathBuffer, &bufferSize) || !PathFileExistsW(exePathBuffer)) {
        delete[] exePathBuffer;
        ShowMessageType(this->hModule, IDS_TITLE_ERROR, IDS_NO_EXECUTABLE_PATH, MB_TOPMOST | MB_ICONERROR);
        return;
    }
    // 此处的字符 'p' 不是传给 HashCalculator 的命令，仅作为占位符，C# 程序接收不到此字符。
    // 因为 C# 程序 Main 函数的 string[] args 参数仅从 CreateProcessW 第二个参数解析得来，
    // 且 C# Main 函数的 string[] args 参数为了不带可执行文件名，CLR 又无脑地删除了解析得到的列表的第一项，
    // 它以为第一项一定是可执行文件名，但在此函数末尾作者根本没有把可执行文件名和命令合并放在 CreateProcessW 第二个参数，
    // 就造成了 C# CLR 错误地把命令行参数的第一项（也就是此处的字符 'p'）当作可执行文件名给删了。
    wstring command_line = wstring(L"p compute");
    if (nullptr != algo) {
        command_line += wstring(L" --algo ") + algo;
    }
    SIZE_T cmd_characters = command_line.length() + 1;
    for (SIZE_T i = 0; i < this->vFilepathList.size(); ++i) {
        if (this->vFilepathList[i].back() == L'\\') {
            this->vFilepathList[i] += L'\\';
        }
        wstring file_path = L" \"" + this->vFilepathList[i] + L"\"";
        SIZE_T file_path_characters = cmd_characters + file_path.length();
        if (file_path_characters < MAX_CMD_CHARS)
        {
            command_line += file_path;
            cmd_characters = file_path_characters;
        }
        else if (file_path_characters > MAX_CMD_CHARS) {
            break;
        }
        else if (file_path_characters == MAX_CMD_CHARS) {
            command_line += file_path;
            cmd_characters = file_path_characters;
            break;
        }
    }
    LPWSTR commandline_buffer = new WCHAR[cmd_characters];
    StringCchCopyW(commandline_buffer, cmd_characters, command_line.c_str());
    STARTUPINFO startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessW(exePathBuffer, commandline_buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL,
        NULL, &startup_info, &proc_info))
    {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
    delete[] commandline_buffer;
}

CComputeHash::CComputeHash() {
    this->hModule = _AtlBaseModule.GetModuleInstance();
    this->hBitmapMenu1 = LoadBitmapW(this->hModule, MAKEINTRESOURCEW(IDB_BITMAP_MENU1));
    this->hBitmapMenu2 = LoadBitmapW(this->hModule, MAKEINTRESOURCEW(IDB_BITMAP_MENU2));
}

CComputeHash::~CComputeHash() {
    DeleteObject(this->hBitmapMenu1);
    DeleteObject(this->hBitmapMenu2);
}

STDMETHODIMP CComputeHash::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    WCHAR filepath_buffer[MAX_PATH];
    this->vFilepathList.clear();
    if (nullptr != pidlFolder) {
        if (SHGetPathFromIDListW(pidlFolder, filepath_buffer)) {
            this->vFilepathList.push_back(filepath_buffer);
            return S_OK;
        }
        return E_INVALIDARG;
    }
    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    STGMEDIUM	stg = { TYMED_HGLOBAL };
    FORMATETC	fmt = {
        CF_HDROP,
        nullptr,
        DVASPECT_CONTENT,
        -1,
        TYMED_HGLOBAL };
    if (FAILED(pdtobj->GetData(&fmt, &stg)))
    {
        return E_INVALIDARG;
    }
    HDROP drop_handle = (HDROP)GlobalLock(stg.hGlobal);
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
            this->vFilepathList.push_back(filepath_buffer);
        }
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}

STDMETHODIMP CComputeHash::QueryContextMenu(
    HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    HMENU submenu_handle = CreatePopupMenu();
    LONG flag = MF_STRING | MF_POPUP;
    ResWString autoAlgoRes = ResWString(this->hModule, IDS_MENU_COMPUTE_AUTO);
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_AUTO, autoAlgoRes.String());
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXH32, L"XXH32");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXH64, L"XXH64");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXH3_64, L"XXH3-64");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXH3_128, L"XXH3-128");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SM3, L"SM3");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_MD5, L"MD5");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_CRC32, L"CRC32");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_CRC64, L"CRC64");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_QUICKXOR, L"QuickXor");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_WHIRLPOOL, L"Whirlpool");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA1, L"SHA-1");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA224, L"SHA-224");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA256, L"SHA-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA384, L"SHA-384");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA512, L"SHA-512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_224, L"SHA3-224");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_256, L"SHA3-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_384, L"SHA3-384");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_512, L"SHA3-512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2B, L"BLAKE2b-512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2BP, L"BLAKE2bp-512");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2S, L"BLAKE2s-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2SP, L"BLAKE2sp-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE3, L"BLAKE3-256");
    AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_STREEBOG_256, L"Streebog-256");
    // 方法退出后 parentMenuRes 会被析构，parentMenuText 会被 delete
    // menu_info.dwTypeData = parentMenuText 安全? InsertMenuItemW 是否复制数据?
    ResWString parentMenuRes = ResWString(this->hModule, IDS_MENU_COMPUTE);
    LPWSTR parentMenuText = parentMenuRes.String();
    MENUITEMINFOW menu_info = { 0 };
    menu_info.cbSize = sizeof(MENUITEMINFOW);
    menu_info.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_TYPE;
    menu_info.fType = MFT_STRING;
    menu_info.wID = idCmdFirst + IDM_COMPUTE_PARENT;
    menu_info.hSubMenu = submenu_handle;
    menu_info.dwTypeData = parentMenuText;
    menu_info.cch = (UINT)wcslen(parentMenuText);
    if (nullptr != this->hBitmapMenu2) {
        menu_info.fMask |= MIIM_CHECKMARKS;
        menu_info.hbmpChecked = this->hBitmapMenu2;
        menu_info.hbmpUnchecked = this->hBitmapMenu2;
    }
    InsertMenuItemW(hmenu, indexMenu + 1, true, &menu_info);
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, IDM_COMPUTE_PARENT + 1);
}

STDMETHODIMP CComputeHash::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    LPCWSTR algo = nullptr;
    switch (LOWORD(pici->lpVerb))
    {
    case IDM_COMPUTE_AUTO:
        break;
    case IDM_COMPUTE_XXH32:
        algo = L"XXH32";
        break;
    case IDM_COMPUTE_XXH64:
        algo = L"XXH64";
        break;
    case IDM_COMPUTE_XXH3_64:
        algo = L"XXH3_64";
        break;
    case IDM_COMPUTE_XXH3_128:
        algo = L"XXH3_128";
        break;
    case IDM_COMPUTE_SM3:
        algo = L"SM3";
        break;
    case IDM_COMPUTE_MD5:
        algo = L"MD5";
        break;
    case IDM_COMPUTE_CRC32:
        algo = L"CRC32";
        break;
    case IDM_COMPUTE_CRC64:
        algo = L"CRC64";
        break;
    case IDM_COMPUTE_QUICKXOR:
        algo = L"QUICKXOR";
        break;
    case IDM_COMPUTE_WHIRLPOOL:
        algo = L"WHIRLPOOL";
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
    case IDM_COMPUTE_BLAKE2B:
        algo = L"BLAKE2B_512";
        break;
    case IDM_COMPUTE_BLAKE2BP:
        algo = L"BLAKE2BP_512";
        break;
    case IDM_COMPUTE_BLAKE2S:
        algo = L"BLAKE2S_256";
        break;
    case IDM_COMPUTE_BLAKE2SP:
        algo = L"BLAKE2SP_256";
        break;
    case IDM_COMPUTE_BLAKE3:
        algo = L"BLAKE3_256";
        break;
    case IDM_COMPUTE_STREEBOG_256:
        algo = L"STREEBOG_256";
        break;
    default:
        return E_INVALIDARG;
    }
    this->CreateGUIProcessComputeHash(algo);
    return S_OK;
}

STDMETHODIMP CComputeHash::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax) {
    return E_NOTIMPL;
}
