// dllmain.h: 模块类的声明。

class CShellExtensionsModule : public ATL::CAtlDllModuleT< CShellExtensionsModule > {
public:
    DECLARE_LIBID(LIBID_ShellExtensionsLib)
    DECLARE_REGISTRY_APPID_RESOURCEID(IDR_SHELLEXTENSIONS, "{18d6b7f2-f466-481f-adfc-849b5f9fbd0b}")
};

extern class CShellExtensionsModule _AtlModule;
