// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include "ContextMenuExt.h"
#include <strsafe.h>
#include <sstream>
#include <shlobj_core.h>

constexpr auto EXEC_NAME = L"HashCalculator.exe";
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
constexpr auto IDM_COMPUTE_SUBMENU = 15;

VOID CContextMenuExt::InitializeModule() {
    HINSTANCE moduleInstance = _AtlBaseModule.GetModuleInstance();
    DWORD initSize = MAX_PATH;
    moduleDir = (WCHAR*)calloc(initSize, sizeof(WCHAR));
    if (nullptr != moduleDir) {
        while (true)
        {
            DWORD size = GetModuleFileNameW(moduleInstance, moduleDir, initSize);
            if (size == initSize || GetLastError() & ERROR_INSUFFICIENT_BUFFER)
            {
                initSize += MAX_PATH;
                moduleDir = (WCHAR*)realloc(moduleDir, initSize);
                if (nullptr == moduleDir) {
                    break;
                }
                continue;
            }
            PathRemoveFileSpecW(moduleDir);
            break;
        }
    }
    hBitmapMenu1 = LoadBitmapW(moduleInstance, MAKEINTRESOURCEW(IDB_BITMAP_MENU1));
    hBitmapMenu2 = LoadBitmapW(moduleInstance, MAKEINTRESOURCEW(IDB_BITMAP_MENU2));
}

VOID CContextMenuExt::CreateGUIProcessComputeHash(const WCHAR* algo) {
    if (nullptr == moduleDir) {
        MessageBoxW(nullptr, L"HashCalculator 扩展模块找不到启动目录", L"错误", MB_TOPMOST | MB_ICONERROR);
        return;
    }
    wstring executablePath = wstring(moduleDir) + L"\\" + EXEC_NAME;
    wstring arguments = L"compute";
    if (nullptr != algo) {
        arguments += wstring(L" --algo ") + algo;
    }
    for (SIZE_T i = 0; i < vwFileNames.size(); ++i) {
        arguments += L" \"" + vwFileNames[i] + L"\"";
    }
    SIZE_T argsWCharCount = arguments.size() + 1;
    WCHAR* mutableArgsBuffer = (WCHAR*)calloc(argsWCharCount, sizeof(WCHAR));
    if (nullptr == mutableArgsBuffer) {
        return;
    }
    StringCchCopyW(mutableArgsBuffer, argsWCharCount, arguments.c_str());
    STARTUPINFO startupInfo = { 0 };
    startupInfo.cb = sizeof(startupInfo);
    PROCESS_INFORMATION procInfo = { 0 };
    if (CreateProcessW(executablePath.c_str(), mutableArgsBuffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
        NULL, NULL, &startupInfo, &procInfo))
    {
        CloseHandle(procInfo.hThread);
        CloseHandle(procInfo.hProcess);
    }
    free(mutableArgsBuffer);
}

STDMETHODIMP CContextMenuExt::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    WCHAR szFilePath[MAX_PATH];
    vwFileNames.clear();
    if (nullptr != pidlFolder) {
        if (SHGetPathFromIDListW(pidlFolder, szFilePath)) {
            vwFileNames.push_back(szFilePath);
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
    HDROP		hDrop = nullptr;
    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    if (FAILED(pdtobj->GetData(&fmt, &stg)))
    {
        return E_INVALIDARG;
    }
    hDrop = (HDROP)GlobalLock(stg.hGlobal);
    if (nullptr == hDrop)
    {
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    uFilesCount = DragQueryFileW(hDrop, INFINITE, nullptr, 0);
    HRESULT hr = S_OK;
    if (0 == uFilesCount)
    {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    for (int index = 0; index < uFilesCount; index++)
    {
        if (0 != DragQueryFileW(hDrop, index, szFilePath, MAX_PATH))
        {
            vwFileNames.push_back(szFilePath);
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
    InsertMenuW(hmenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP,
        idCmdFirst + IDM_COMPUTE_HASH, L"计算所选对象哈希值");
    if (hBitmapMenu1 != nullptr) {
        SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, hBitmapMenu1, hBitmapMenu1);
    }
    HMENU hSubMenus = CreatePopupMenu();
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA1, L"SHA1");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA224, L"SHA224");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA256, L"SHA256");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA384, L"SHA384");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA512, L"SHA512");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA3_224, L"SHA3-224");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA3_256, L"SHA3-256");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA3_384, L"SHA3-384");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_SHA3_512, L"SHA3-512");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_MD5, L"MD5");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_BLAKE2S, L"BLAKE2s");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_BLAKE2B, L"BLAKE2b");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_BLAKE3, L"BLAKE3");
    AppendMenuW(hSubMenus, MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTE_WHIRLPOOL, L"Whirlpool");
    LPWSTR subMenuName = L"执行指定的哈希算法";
    SIZE_T subMenuNameLen = wcslen(subMenuName);
    MENUITEMINFOW menuInfo = { 0 };
    menuInfo.cbSize = sizeof(MENUITEMINFOW);
    menuInfo.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_TYPE;
    menuInfo.fType = MFT_STRING;
    menuInfo.wID = idCmdFirst + IDM_COMPUTE_SUBMENU;
    menuInfo.hSubMenu = hSubMenus;
    menuInfo.dwTypeData = subMenuName;
    menuInfo.cch = (UINT)subMenuNameLen;
    if (nullptr != hBitmapMenu2) {
        menuInfo.fMask |= MIIM_CHECKMARKS;
        menuInfo.hbmpChecked = hBitmapMenu2;
        menuInfo.hbmpUnchecked = hBitmapMenu2;
    }
    InsertMenuItemW(hmenu, indexMenu + 1, true, &menuInfo);
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, USHORT(IDM_COMPUTE_SUBMENU + 1));
}

STDMETHODIMP CContextMenuExt::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    WCHAR* algo = nullptr;
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
