#include "pch.h"
#include "commons.h"
#include "ResString.h"
#include "tiny-json.h"
#include <map>
#include <string>
#include <strsafe.h>
#include <Windows.h>

using std::pair;
using std::map;
using std::string;

/// <summary>
/// 从注册表读取 HashCalculator.exe 的路径，此路径在安装系统快捷菜单扩展时被写入注册表。
/// </summary>
BOOL GetHashCalculatorPath(LPSTR* buffer, LPDWORD bufsize) {
    BOOL executionResult = FALSE;
    HKEY hKeyHcAppPath = nullptr;
    HKEY hKeyCurrentUser = nullptr;
    if (ERROR_SUCCESS != RegOpenCurrentUser(KEY_READ, &hKeyCurrentUser) ||
        ERROR_SUCCESS != RegOpenKeyExA(hKeyCurrentUser, HCEXE_REGPATH, 0, KEY_READ, &hKeyHcAppPath)) {
        if (ERROR_SUCCESS != RegOpenKeyExA(HKEY_LOCAL_MACHINE, HCEXE_REGPATH, 0, KEY_READ, &hKeyHcAppPath)) {
            goto FinalizeAndReturn;
        }
    }
    DWORD valueDataType;
    LSTATUS status1 = RegGetValueA(hKeyHcAppPath, NULL, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
    if (ERROR_MORE_DATA == status1) {
        delete[] * buffer;
        *buffer = new CHAR[*bufsize]();
        LSTATUS status2 = RegGetValueA(hKeyHcAppPath, NULL, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
        executionResult = ERROR_SUCCESS == status2 && REG_SZ == valueDataType;
    }
    else {
        executionResult = ERROR_SUCCESS == status1 && REG_SZ == valueDataType;
    }
FinalizeAndReturn:
    if (nullptr != hKeyHcAppPath) {
        RegCloseKey(hKeyHcAppPath);
    }
    if (nullptr != hKeyCurrentUser) {
        RegCloseKey(hKeyCurrentUser);
    }
    return executionResult;
}

VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType) {
    ResString title = ResString(hModule, titleID);
    ResString message = ResString(hModule, messageID);
    MessageBoxA(nullptr, message.String(), title.String(), uType);
}

static BOOL json_getPropValueByType(const json_t* parent, const char* propName, jsonType_t jsonType, void* addr) {
    if (NULL != parent && NULL != propName) {
        const json_t* propJson = json_getProperty(parent, propName);
        if (NULL != propJson && jsonType == json_getType(propJson)) {
            switch (jsonType) {
            case JSON_BOOLEAN:
            {
                bool* boolAddr = (bool*)addr;
                *boolAddr = json_getBoolean(propJson);
                break;
            }
            case JSON_INTEGER:
            {
                int64_t* int64Addr = (int64_t*)addr;
                *int64Addr = json_getInteger(propJson);
                break;
            }
            case JSON_TEXT:
            {
                const char** strAddr = (const char**)addr;
                *strAddr = json_getValue(propJson);
                break;
            }
            default:
                return FALSE;
            }
            return TRUE;
        }
    }
    return FALSE;
}

BOOL InsertMenuFromJsonFile(const CHAR* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast,
    MenuType_t menuType, UINT* pIdCurrent, map<UINT, CHAR*>& mCmdDict, HBITMAP bitMapHandle) {
    LPSTR ansiData = NULL;
    LPWSTR unicodeData = NULL;
    UINT initalId = *pIdCurrent;
    UINT indexTopCurrent = indexMenu;
    FILE* jsonFile = NULL;
    json_t* jsonMemory = NULL;
    DeleteCmdDictBuffer(mCmdDict);
    mCmdDict.clear();
    if (menuType == MENUTYPE_UNKNOWN) {
        return FALSE;
    }
    errno_t error = fopen_s(&jsonFile, menuJson, "rb");
    if (error != 0 || NULL == jsonFile) {
        goto FinalizeAndReturn;
    }
    fseek(jsonFile, 0L, SEEK_END);
    SIZE_T fileSize = ftell(jsonFile);
    if (fileSize < 2 || fileSize % sizeof(WCHAR) != 0) {
        goto FinalizeAndReturn;
    }
    // 验证 UTF-16LE 编码的文件头部 2 字节是否是 FFFE
    const SIZE_T headCount = 2;
    CHAR utf16leHead[headCount] = { 0 };
    rewind(jsonFile);
    if (headCount != fread(utf16leHead, sizeof(CHAR), headCount, jsonFile)) {
        goto FinalizeAndReturn;
    }
    // 在小端字节序下 FFFE 两个字节对应的数值是 0xFEFF
    if (0xFEFFu != *((WORD*)utf16leHead)) {
        goto FinalizeAndReturn;
    }
    SIZE_T wchCount = fileSize / sizeof(WCHAR);
    // 此处给 unicodeData 分配空间时不用为字符串末尾的 L'\0' 而 wchCount + 1，
    // 因为 UTF-16LE 编码文件头有 2 字节的标识 FFFE，不读取这两个字节省下的空间刚好够 L'\0'。
    unicodeData = new WCHAR[wchCount];
    unicodeData[wchCount - 1] = 0;
    SIZE_T expectedCount = fileSize - headCount;
    SIZE_T byteCountRead = fread(unicodeData, sizeof(CHAR), expectedCount, jsonFile);
    if (byteCountRead != expectedCount) {
        goto FinalizeAndReturn;
    }
    INT reqSize = WideCharToMultiByte(CP_ACP, 0, unicodeData, -1, NULL, 0, NULL, NULL);
    if (0 == reqSize) {
        goto FinalizeAndReturn;
    }
    ansiData = new CHAR[reqSize];
    ansiData[reqSize - 1] = 0;
    if (0 == WideCharToMultiByte(CP_ACP, 0, unicodeData, -1, ansiData, reqSize, NULL, NULL)) {
        goto FinalizeAndReturn;
    }
    jsonMemory = new json_t[MAX_JSON_PROP];
    const json_t* topListJson = json_create(ansiData, jsonMemory, MAX_JSON_PROP);
    if (NULL == topListJson || JSON_ARRAY != json_getType(topListJson)) {
        goto FinalizeAndReturn;
    }
    const json_t* topMenuJson = NULL;
    for (topMenuJson = json_getChild(topListJson); topMenuJson; topMenuJson = json_getSibling(topMenuJson)) {
        int64_t i64MenuType;
        const char* topMenuTitle;
        if (!json_getPropValueByType(topMenuJson, JSON_MENUTYPE, JSON_INTEGER, &i64MenuType)
            || i64MenuType != (int64_t)menuType
            || !json_getPropValueByType(topMenuJson, JSON_TITLE, JSON_TEXT, &topMenuTitle)) {
            continue;
        }
        MENUITEMINFOA topMenuOrSubmenuContainerInfo = { 0 };
        topMenuOrSubmenuContainerInfo.fMask = MIIM_ID | MIIM_STRING | MIIM_BITMAP;
        topMenuOrSubmenuContainerInfo.cbSize = sizeof(topMenuOrSubmenuContainerInfo);
        topMenuOrSubmenuContainerInfo.wID = idCmdFirst + *pIdCurrent;
        topMenuOrSubmenuContainerInfo.cch = (UINT)strlen(topMenuTitle);
        topMenuOrSubmenuContainerInfo.dwTypeData = (LPSTR)topMenuTitle;
        topMenuOrSubmenuContainerInfo.hbmpItem = bitMapHandle;
        const json_t* subListJson = json_getProperty(topMenuJson, JSON_SUBMENUS);
        if (NULL == subListJson) {
            const char* usingAlgsStr;
            if (json_getPropValueByType(topMenuJson, JSON_ALGTYPES, JSON_TEXT, &usingAlgsStr)
                && InsertMenuItemA(hMenu, indexTopCurrent, true, &topMenuOrSubmenuContainerInfo)) {
                ++indexTopCurrent;
                SIZE_T bufferCharCapacity = strlen(usingAlgsStr) + 1;
                CHAR* algoTypeBuffer = new char[bufferCharCapacity]();
                StringCchCopyA(algoTypeBuffer, bufferCharCapacity, usingAlgsStr);
                mCmdDict.emplace(*pIdCurrent, algoTypeBuffer);
                *pIdCurrent = *pIdCurrent + 1;
            }
        }
        else if (JSON_ARRAY == json_getType(subListJson)) {
            HMENU hSubmenuContainer = CreatePopupMenu();
            topMenuOrSubmenuContainerInfo.hSubMenu = hSubmenuContainer;
            topMenuOrSubmenuContainerInfo.fMask |= MIIM_SUBMENU;
            UINT appendedSubmenuCount = 0U;
            LONG flag = MF_STRING | MF_POPUP;
            const json_t* submenuJson = NULL;
            for (submenuJson = json_getChild(subListJson); submenuJson; submenuJson = json_getSibling(submenuJson)) {
                const char* submenuTitle, * submenuUsingAlgsStr;
                if (json_getPropValueByType(submenuJson, JSON_TITLE, JSON_TEXT, &submenuTitle)
                    && json_getPropValueByType(submenuJson, JSON_ALGTYPES, JSON_TEXT, &submenuUsingAlgsStr)
                    && AppendMenuA(hSubmenuContainer, flag, idCmdFirst + *pIdCurrent, submenuTitle)) {
                    ++appendedSubmenuCount;
                    SIZE_T bufferCharCapacity = strlen(submenuUsingAlgsStr) + 1;
                    CHAR* submenuAlgoTypeBuffer = new char[bufferCharCapacity]();
                    StringCchCopyA(submenuAlgoTypeBuffer, bufferCharCapacity, submenuUsingAlgsStr);
                    mCmdDict.emplace(*pIdCurrent, submenuAlgoTypeBuffer);
                    *pIdCurrent = *pIdCurrent + 1;
                }
            }
            if (0 != appendedSubmenuCount && InsertMenuItemA(hMenu, indexTopCurrent, true, &topMenuOrSubmenuContainerInfo)) {
                ++indexTopCurrent;
                *pIdCurrent = *pIdCurrent + 1;
                continue;
            }
            DestroyMenu(hSubmenuContainer);
        }
    }
FinalizeAndReturn:
    if (NULL != jsonFile) {
        fclose(jsonFile);
    }
    if (NULL != jsonMemory) {
        delete[] jsonMemory;
    }
    if (NULL != ansiData) {
        delete[] ansiData;
    }
    if (NULL != unicodeData) {
        delete[] unicodeData;
    }
    return initalId != *pIdCurrent;
}

VOID DeleteCmdDictBuffer(map<UINT, CHAR*>& mCmdDict) {
    for (pair<const UINT, CHAR*>& keyValuePair : mCmdDict) {
        delete[] keyValuePair.second;
        keyValuePair.second = nullptr;
    }
}
