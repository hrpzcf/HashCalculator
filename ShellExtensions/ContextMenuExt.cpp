// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include "ContextMenuExt.h"
#include <shlobj_core.h>

constexpr auto IDM_COMPUTEHASH = 0;

VOID CContextMenuExt::InitializeMenuIcon() {
    hbmMenuIcon = LoadBitmapW(
        _AtlBaseModule.GetModuleInstance(), MAKEINTRESOURCEW(IDB_BITMAP_MENU));
}

VOID CContextMenuExt::CreateComputeHashProcess() {
    
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
    HMENU hmenu,
    UINT indexMenu,
    UINT idCmdFirst,
    UINT idCmdLast,
    UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, USHORT(0));
    }
    InsertMenuW(hmenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP, idCmdFirst + IDM_COMPUTEHASH, L"计算选区文件哈希值");
    if (hbmMenuIcon != nullptr) {
        SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, hbmMenuIcon, hbmMenuIcon);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, USHORT(IDM_COMPUTEHASH + 1));
}

STDMETHODIMP CContextMenuExt::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    switch (LOWORD(pici->lpVerb))
    {
    case IDM_COMPUTEHASH:
        CreateComputeHashProcess();
        break;
    default:
        return E_INVALIDARG;
    }
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
