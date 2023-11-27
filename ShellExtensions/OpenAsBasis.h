// OpenAsBasis.h: COpenAsBasis 的声明

#pragma once
#include "resource.h"       // 主符号
#include "ShellExtensions_i.h"
#include <vector>
#include <string>

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Windows CE 平台(如不提供完全 DCOM 支持的 Windows Mobile 平台)上无法正确支持单线程 COM 对象。定义 _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA 可强制 ATL 支持创建单线程 COM 对象实现并允许使用其单线程 COM 对象实现。rgs 文件中的线程模型已被设置为“Free”，原因是该模型是非 DCOM Windows CE 平台支持的唯一线程模型。"
#endif

using namespace ATL;
using std::vector;
using std::wstring;

class ATL_NO_VTABLE COpenAsBasis :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<COpenAsBasis, &CLSID_OpenAsBasis>,
    public IDispatchImpl<IOpenAsBasis, &IID_IOpenAsBasis, &LIBID_ShellExtensionsLib, /*wMajor =*/ 1, /*wMinor =*/ 0>,
    public IShellExtInit,
    public IContextMenu
{
    HBITMAP bitmap_menu = nullptr;
    LPWSTR basis_path = nullptr;
    LPWSTR executable_path = nullptr;
    HINSTANCE module_inst = nullptr;
    VOID CreateGUIProcessVerifyHash(LPWSTR);
public:
    COpenAsBasis();
    ~COpenAsBasis();

    DECLARE_REGISTRY_RESOURCEID(IDR_OPENASBASIS)

    BEGIN_COM_MAP(COpenAsBasis)
        COM_INTERFACE_ENTRY(IOpenAsBasis)
        COM_INTERFACE_ENTRY(IDispatch)
        COM_INTERFACE_ENTRY(IShellExtInit)
        COM_INTERFACE_ENTRY(IContextMenu)
    END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT();

    HRESULT FinalConstruct() { return S_OK; }
    void FinalRelease() {  }
    STDMETHOD(Initialize)(PCIDLIST_ABSOLUTE, IDataObject*, HKEY);
    STDMETHOD(QueryContextMenu)(HMENU, UINT, UINT, UINT, UINT);
    STDMETHOD(InvokeCommand)(CMINVOKECOMMANDINFO*);
    STDMETHOD(GetCommandString)(UINT_PTR, UINT, UINT*, CHAR*, UINT);
};

OBJECT_ENTRY_AUTO(__uuidof(OpenAsBasis), COpenAsBasis)
