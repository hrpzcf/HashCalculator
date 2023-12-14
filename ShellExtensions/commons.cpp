#include "pch.h"
#include <Windows.h>
#include "commons.h"
#include "ResString.h"

/// <summary>
/// 从注册表读取 HashCalculator.exe 的路径，此路径在安装系统快捷菜单扩展时被写入注册表。
/// </summary>
BOOL GetHashCalculatorPath(LPWSTR* buffer, LPDWORD bufsize) {
    BOOL executionResult = FALSE;
    HKEY keyAppPaths = nullptr;
    HKEY keyCurrentUser = nullptr;
    if (ERROR_SUCCESS != RegOpenCurrentUser(KEY_READ, &keyCurrentUser)) {
        goto FinalizeAndReturn;
    }
    if (ERROR_SUCCESS != RegOpenKeyExW(keyCurrentUser, HCEXE_REGPATH, 0, KEY_READ, &keyAppPaths)) {
        goto FinalizeAndReturn;
    }
    DWORD valueDataType;
    LSTATUS status1 = RegGetValueW(keyAppPaths, HC_EXECUTABLE, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
    if (ERROR_MORE_DATA == status1) {
        delete[] * buffer;
        *buffer = new WCHAR[*bufsize]();
        LSTATUS status2 = RegGetValueW(keyAppPaths, HC_EXECUTABLE, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
        executionResult = ERROR_SUCCESS == status2 && REG_SZ == valueDataType;
    }
    else
    {
        executionResult = ERROR_SUCCESS == status1 && REG_SZ == valueDataType;
    }
FinalizeAndReturn:
    if (nullptr != keyAppPaths) {
        RegCloseKey(keyAppPaths);
    }
    if (nullptr != keyCurrentUser) {
        RegCloseKey(keyCurrentUser);
    }
    return executionResult;
}

VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType) {
    ResWString title = ResWString(hModule, titleID);
    ResWString message = ResWString(hModule, messageID);
    MessageBoxW(nullptr, message.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
}
