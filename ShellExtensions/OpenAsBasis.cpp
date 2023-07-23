// OpenAsBasis.cpp: COpenAsBasis 的实现

#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "OpenAsBasis.h"
#include "ResString.h"

constexpr auto IDM_VERIFY_HASH = 0;

VOID COpenAsBasis::CreateGUIProcessVerifyHash() {
    if (nullptr == this->basis_path) {
        return;
    }
    if (nullptr == this->executable_path) {
        CResStringW title = CResStringW(this->module_inst, IDS_TITLE_ERROR);
        CResStringW text = CResStringW(this->module_inst, IDS_NO_EXECUTABLE_PATH);
        MessageBoxW(nullptr, text.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
        return;
    }
    LPCWSTR format = L"%s verify -b \"%s\"";
    SIZE_T cch_cmd = wcslen(format) + wcslen(EXECUTABLE) + wcslen(this->basis_path);
    if (cch_cmd > MAX_CMD_CHARS) {
        return;
    }
    LPWSTR buffer;
    try {
        buffer = new WCHAR[cch_cmd]; // 无需 +1：wcslen(format) 已经多算了 %s
    }
    catch (const std::bad_alloc&) {
        return;
    }
    if (FAILED(StringCchPrintfW(buffer, cch_cmd, format, EXECUTABLE, this->basis_path))) {
        return;
    }
    STARTUPINFO startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessW(this->executable_path, buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
        NULL, NULL, &startup_info, &proc_info))
    {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
    delete[] buffer;
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
    CResStringW resource = CResStringW(this->module_inst, IDS_MENU_VERIFY);
    InsertMenuW(hmenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP,
        idCmdFirst + IDM_VERIFY_HASH, resource.String());
    if (this->bitmap_menu != nullptr) {
        SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, this->bitmap_menu, this->bitmap_menu);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, IDM_VERIFY_HASH + 1);
}

STDMETHODIMP COpenAsBasis::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    switch (LOWORD(pici->lpVerb))
    {
    case IDM_VERIFY_HASH:
        this->CreateGUIProcessVerifyHash();
        return S_OK;
    }
    return E_INVALIDARG;
}

STDMETHODIMP COpenAsBasis::GetCommandString(
    UINT_PTR idCmd,
    UINT uType,
    UINT* pReserved,
    _Out_writes_bytes_((uType& GCS_UNICODE) ? (cchMax * sizeof(wchar_t)) : cchMax) _When_(!(uType& (GCS_VALIDATEA | GCS_VALIDATEW)), _Null_terminated_) CHAR* pszName,
    UINT cchMax) {
    return E_NOTIMPL;
}
