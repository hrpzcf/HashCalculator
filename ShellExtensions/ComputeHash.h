// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#pragma once
#include "resource.h"
#include "ShellExtensions_i.h"
#include <map>
#include <string>
#include <vector>

using namespace ATL;
using std::map;
using std::vector;
using std::string;


class ATL_NO_VTABLE CComputeHash :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CComputeHash, &CLSID_ComputeHash>,
    public IDispatchImpl<IComputeHash, &IID_IComputeHash, &LIBID_ShellExtensionsLib, /*wMajor =*/ 1, /*wMinor =*/ 0>,
    public IShellExtInit,
    public IContextMenu {
    LPSTR MenuJsonPath = nullptr;
    HINSTANCE hModule = nullptr;
    HBITMAP hBitmapMenu = nullptr;
    map<UINT, CHAR*> mCmdDict;
    vector<string> vFilepathList;
    VOID CreateGUIProcessComputeHash(LPCSTR);

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
