#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "OpenAsChecklist.h"
#include "ResString.h"


VOID COpenAsChecklist::CreateGUIProcessVerifyHash(LPCSTR algo) const {
    if (nullptr == this->checklistPath) {
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
    string command_line = string("p verify");
    if (nullptr != algo && 0 != strlen(algo)) {
        command_line += string(" --algo ") + algo;
    }
    command_line += " --list";
    // 为什么 +4：checklist_path 前面 1 个空格和前后 2 个引号，1 个终止字符
    SIZE_T cmd_characters = command_line.length() + strlen(this->checklistPath) + 4;
    if (cmd_characters > MAX_CMD_CHARS) {
        return;
    }
    command_line += " \"" + string(this->checklistPath) + "\"";
    LPSTR commandlineBuffer = new CHAR[cmd_characters];
    StringCchCopyA(commandlineBuffer, cmd_characters, command_line.c_str());
    STARTUPINFOA startup_info = { 0 };
    startup_info.cb = sizeof(startup_info);
    PROCESS_INFORMATION proc_info = { 0 };
    if (CreateProcessA(exePathBuffer, commandlineBuffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS, NULL,
        NULL, &startup_info, &proc_info))
    {
        CloseHandle(proc_info.hThread);
        CloseHandle(proc_info.hProcess);
    }
    delete[] exePathBuffer;
    delete[] commandlineBuffer;
}

COpenAsChecklist::COpenAsChecklist() {
    this->hModule = _AtlBaseModule.GetModuleInstance();
    this->hBitmapMenu = LoadBitmapW(this->hModule, MAKEINTRESOURCEW(IDB_BITMAP_MENU3));
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

COpenAsChecklist::~COpenAsChecklist() {
    DeleteObject(this->hBitmapMenu);
    DeleteCmdDictBuffer(this->mCmdDict);
    delete[] this->MenuJsonPath;
    delete[] this->checklistPath;
}

STDMETHODIMP COpenAsChecklist::Initialize(
    PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
    if (nullptr == pdtobj) {
        return E_INVALIDARG;
    }
    delete[] this->checklistPath;
    this->checklistPath = nullptr;
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
    if (1 != DragQueryFileW(drop_handle, INFINITE, nullptr, 0))
    {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    UINT chars = DragQueryFileA(drop_handle, 0, nullptr, 0) + 1;
    if (1 >= chars) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        return E_INVALIDARG;
    }
    this->checklistPath = new CHAR[chars];
    if (0 == DragQueryFileA(drop_handle, 0, this->checklistPath, chars)) {
        GlobalUnlock(stg.hGlobal);
        ReleaseStgMedium(&stg);
        delete[] this->checklistPath;
        this->checklistPath = nullptr;
        return E_FAIL;
    }
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);
    return S_OK;
}

STDMETHODIMP COpenAsChecklist::QueryContextMenu(
    HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
    if (uFlags & CMF_DEFAULTONLY)
    {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    UINT idCmdCurrent = 0;
    if (!InsertMenuFromJsonFile(this->MenuJsonPath, hMenu, indexMenu, idCmdFirst, idCmdLast, MENUTYPE_CHECKHASH,
        &idCmdCurrent, this->mCmdDict, this->hBitmapMenu)) {
        return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
    }
    return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, idCmdCurrent);
}

STDMETHODIMP COpenAsChecklist::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
    if (0 != HIWORD(pici->lpVerb))
    {
        return E_INVALIDARG;
    }
    map<UINT, char*>::iterator iter = mCmdDict.find(LOWORD(pici->lpVerb));
    if (iter == mCmdDict.end())
    {
        return E_INVALIDARG;
    }
    this->CreateGUIProcessVerifyHash(iter->second);
    return S_OK;
}

STDMETHODIMP COpenAsChecklist::GetCommandString(UINT_PTR idCmd, UINT uType, UINT* pReserved, CHAR* pszName, UINT cchMax) {
    return E_NOTIMPL;
}
