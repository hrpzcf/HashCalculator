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


class ATL_NO_VTABLE COpenAsChecklist :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<COpenAsChecklist, &CLSID_OpenAsChecklist>,
    public IDispatchImpl<IOpenAsChecklist, &IID_IOpenAsChecklist, &LIBID_ShellExtensionsLib, /*wMajor =*/ 1, /*wMinor =*/ 0>,
    public IShellExtInit,
    public IContextMenu {
    LPSTR MenuJsonPath = nullptr;
    HINSTANCE hModule = nullptr;
    LPSTR checklistPath = nullptr;
    HBITMAP hBitmapMenu = nullptr;
    map<UINT, CHAR*> mCmdDict;
    VOID CreateGUIProcessVerifyHash(LPCSTR) const;
public:
    COpenAsChecklist();
    ~COpenAsChecklist();

    DECLARE_REGISTRY_RESOURCEID(IDR_OPENASCHECKLIST)

    BEGIN_COM_MAP(COpenAsChecklist)
        COM_INTERFACE_ENTRY(IOpenAsChecklist)
        COM_INTERFACE_ENTRY(IDispatch)
        COM_INTERFACE_ENTRY(IShellExtInit)
        COM_INTERFACE_ENTRY(IContextMenu)
    END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT();

    HRESULT FinalConstruct() {
        return S_OK;
    }
    void FinalRelease() {
    }
    STDMETHOD(Initialize)(PCIDLIST_ABSOLUTE, IDataObject*, HKEY);
    STDMETHOD(QueryContextMenu)(HMENU, UINT, UINT, UINT, UINT);
    STDMETHOD(InvokeCommand)(CMINVOKECOMMANDINFO*);
    STDMETHOD(GetCommandString)(UINT_PTR, UINT, UINT*, CHAR*, UINT);
};

OBJECT_ENTRY_AUTO(__uuidof(OpenAsChecklist), COpenAsChecklist)
