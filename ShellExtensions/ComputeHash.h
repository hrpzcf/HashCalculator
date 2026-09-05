// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#pragma once
#include "resource.h"
#include "ShellExtensions_i.h"
#include <atlbase.h>
#include <atlcom.h>
#include <atldef.h>
#include <map>
#include <ocidl.h>
#include <ShObjIdl_core.h>
#include <shtypes.h>
#include <string>
#include <vector>
#include <Windows.h>

using namespace ATL;
using std::map;
using std::vector;
using std::wstring;


class ATL_NO_VTABLE CComputeHash :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CComputeHash, &CLSID_ComputeHash>,
    public IDispatchImpl<IComputeHash, &IID_IComputeHash, &LIBID_ShellExtensionsLib, /*wMajor =*/ 1, /*wMinor =*/ 0>,
    public IShellExtInit,
    public IContextMenu {
    wstring MenuJsonPath;
    HINSTANCE hModule = nullptr;
    HBITMAP hBitmapMenu = nullptr;
    bool mIsBackgroundContext = false;
    map<UINT, wstring> mIDCmdToAlgos;
    vector<wstring> vFilepathList;
    VOID CreateGUIProcessComputeHash(const wstring&);

public:
    CComputeHash();
    ~CComputeHash();

    DECLARE_REGISTRY_RESOURCEID(IDR_COMPUTEHASH)

    BEGIN_COM_MAP(CComputeHash)
        COM_INTERFACE_ENTRY(IComputeHash)
        COM_INTERFACE_ENTRY(IDispatch)
        COM_INTERFACE_ENTRY(IShellExtInit)
        COM_INTERFACE_ENTRY(IContextMenu)
    END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT();

    HRESULT FinalConstruct() { return S_OK; }
    VOID FinalRelease() { }
    STDMETHOD(Initialize)(PCIDLIST_ABSOLUTE, IDataObject*, HKEY);
    STDMETHOD(QueryContextMenu)(HMENU, UINT, UINT, UINT, UINT);
    STDMETHOD(InvokeCommand)(CMINVOKECOMMANDINFO*);
    STDMETHOD(GetCommandString)(UINT_PTR, UINT, UINT*, CHAR*, UINT);
};

OBJECT_ENTRY_AUTO(__uuidof(ComputeHash), CComputeHash)
