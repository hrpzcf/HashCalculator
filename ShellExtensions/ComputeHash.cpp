// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "ComputeHash.h"
#include "ResString.h"


VOID CComputeHash::CreateGUIProcessComputeHash(LPCSTR algo) {
    if (this->vFilepathList.empty()) {
        return;
    }
    DWORD bufferSize = MAX_PATH;
    LPSTR exePathBuffer = new CHAR[bufferSize]();
    if (!GetHashCalculatorPath(&exePathBuffer, &bufferSize) || !PathFileExistsA(exePathBuffer)) {
        delete[] exePathBuffer;
        ShowMessageType(this->hModule, IDS_TITLE_ERROR, IDS_NO_EXECUTABLE_PATH, MB_TOPMOST | MB_ICONERROR);
        return;
    }
    // 此处的字符 'p' 不是传给 HashCalculator 的命令，仅作为占位符，C# 程序接收不到此字符。
    // 因为 C# 程序 Main 函数的 string[] args 参数仅从 CreateProcessW 第二个参数解析得来，
    // 且 C# Main 函数的 string[] args 参数为了不带可执行文件名，CLR 又无脑地删除了解析得到的列表的第一项，
    // 它以为第一项一定是可执行文件名，但在此函数末尾作者根本没有把可执行文件名和命令合并放在 CreateProcessW 第二个参数，
    // 就造成了 C# CLR 错误地把命令行参数的第一项（也就是此处的字符 'p'）当作可执行文件名给删了。
    string command_line = string("p compute");
    if (nullptr != algo && 0 != strlen(algo)) {
        command_line += string(" --algo ") + algo;
    }
    SIZE_T cmd_characters = command_line.length() + 1;
    for (SIZE_T i = 0; i < this->vFilepathList.size(); ++i) {
        if (this->vFilepathList[i].back() == L'\\') {
            this->vFilepathList[i] += L'\\';
        }
        string file_path = " \"" + this->vFilepathList[i] + "\"";
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
    LPSTR commandline_buffer = new CHAR[cmd_characters];
    StringCchCopyA(commandline_buffer, cmd_characters, command_line.c_str());
    STARTUPINFOA startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessA(exePathBuffer, commandline_buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL,
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
    DWORD bufsize = MAX_PATH;
    LPSTR  moduleDirPath = new CHAR[bufsize]();
    while (true)
    {
        GetModuleFileNameA(this->hModule, moduleDirPath, bufsize);
        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
            delete[]  moduleDirPath;
            bufsize += MAX_PATH;
            moduleDirPath = new CHAR[bufsize]();
            continue;
        }
        if (!PathRemoveFileSpecA(moduleDirPath)) {
            break;
        }
        SIZE_T pathChLength = strlen(moduleDirPath);
        if (pathChLength == 0) {
            break;
        }
        SIZE_T menuJsonPathTotalChLength = pathChLength + 2 + strlen(MENU_JSONNAME);
        this->MenuJsonPath = new CHAR[menuJsonPathTotalChLength]();
        StringCchCatA(this->MenuJsonPath, menuJsonPathTotalChLength, moduleDirPath);
        StringCchCatA(this->MenuJsonPath, menuJsonPathTotalChLength, "\\");
        StringCchCatA(this->MenuJsonPath, menuJsonPathTotalChLength, MENU_JSONNAME);
        break;
    }
    delete[] moduleDirPath;
}

CComputeHash::~CComputeHash() {
    DeleteObject(this->hBitmapMenu1);
    DeleteObject(this->hBitmapMenu2);
    DeleteCmdDictBuffer(this->mCmdDict);
    delete[] this->MenuJsonPath;
}

STDMETHODIMP CComputeHash::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    CHAR filepath_buffer[MAX_PATH];
    this->vFilepathList.clear();
    if (nullptr != pidlFolder) {
        if (SHGetPathFromIDListA(pidlFolder, filepath_buffer)) {
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
    UINT file_count = DragQueryFileA(drop_handle, INFINITE, nullptr, 0);
    if (0 == file_count)
    {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    for (UINT index = 0; index < file_count; index++)
    {
        if (0 != DragQueryFileA(drop_handle, index, filepath_buffer, MAX_PATH))
        {
            this->vFilepathList.push_back(filepath_buffer);
        }
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}

STDMETHODIMP CComputeHash::QueryContextMenu(
    HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    UINT idCmdCurrent = 0;
    if (!InsertMenuFromJsonFile(this->MenuJsonPath, hMenu, indexMenu, idCmdFirst, idCmdLast, MENUTYPE_COMPUTE,
        &idCmdCurrent, this->mCmdDict, this->hBitmapMenu1)) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, idCmdCurrent);
}

STDMETHODIMP CComputeHash::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    map<UINT, char*>::iterator iter = mCmdDict.find(LOWORD(pici->lpVerb));
    if (iter == mCmdDict.end())
    {
        return E_INVALIDARG;
    }
    this->CreateGUIProcessComputeHash(iter->second);
    return S_OK;
}

STDMETHODIMP CComputeHash::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax) {
    return E_NOTIMPL;
}
