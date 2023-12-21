#include "pch.h"
#include <Windows.h>
#include <map>
#include <string>
#include <strsafe.h>
#include "tiny-json.h"
#include "commons.h"
#include "ResString.h"

using std::pair;
using std::map;
using std::string;

/// <summary>
/// 从注册表读取 HashCalculator.exe 的路径，此路径在安装系统快捷菜单扩展时被写入注册表。
/// </summary>
BOOL GetHashCalculatorPath(LPSTR* buffer, LPDWORD bufsize) {
    BOOL executionResult = FALSE;
    HKEY keyAppPaths = nullptr;
    HKEY keyCurrentUser = nullptr;
    if (ERROR_SUCCESS != RegOpenCurrentUser(KEY_READ, &keyCurrentUser)) {
        goto FinalizeAndReturn;
    }
    if (ERROR_SUCCESS != RegOpenKeyExA(keyCurrentUser, HCEXE_REGPATH, 0, KEY_READ, &keyAppPaths)) {
        goto FinalizeAndReturn;
    }
    DWORD valueDataType;
    LSTATUS status1 = RegGetValueA(keyAppPaths, HC_EXECUTABLE, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
    if (ERROR_MORE_DATA == status1) {
        delete[] * buffer;
        *buffer = new CHAR[*bufsize]();
        LSTATUS status2 = RegGetValueA(keyAppPaths, HC_EXECUTABLE, NULL, RRF_RT_REG_SZ, &valueDataType, *buffer, bufsize);
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
    ResString title = ResString(hModule, titleID);
    ResString message = ResString(hModule, messageID);
    MessageBoxA(nullptr, message.String(), title.String(), uType);
}

static BOOL json_getPropValueByType(const json_t* parent, const char* propName, jsonType_t jsonType, void* addr) {
    if (NULL != parent && NULL != propName) {
        const json_t* propJson = json_getProperty(parent, propName);
        if (NULL != propJson && jsonType == json_getType(propJson)) {
            switch (jsonType)
            {
            case JSON_BOOLEAN: {
                bool* boolAddr = (bool*)addr;
                *boolAddr = json_getBoolean(propJson);
                break;
            }
            case JSON_INTEGER: {
                int64_t* int64Addr = (int64_t*)addr;
                *int64Addr = json_getInteger(propJson);
                break;
            }
            case JSON_TEXT: {
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
    json_t* jsonMemory = NULL;
    CHAR* jsonCharData = NULL;
    UINT initalId = *pIdCurrent;
    UINT indexTopCurrent = indexMenu;
    DeleteCmdDictBuffer(mCmdDict);
    mCmdDict.clear();
    if (menuType == MENUTYPE_UNKNOWN) {
        return FALSE;
    }
    FILE* jsonFile;
    errno_t error = fopen_s(&jsonFile, menuJson, "rb");
    if (error != 0 || NULL == jsonFile) {
        goto FinalizeAndReturn;
    }
    fseek(jsonFile, 0L, SEEK_END);
    SIZE_T fileSize = ftell(jsonFile);
    jsonCharData = new CHAR[fileSize + 1];
    rewind(jsonFile);
    SIZE_T elementCount = 1;
    SIZE_T readEleCount = fread(jsonCharData, fileSize, elementCount, jsonFile);
    fclose(jsonFile);
    if (readEleCount != elementCount) {
        goto FinalizeAndReturn;
    }
    jsonMemory = new json_t[MAX_JSON_PROP];
    const json_t* topListJson = json_create(jsonCharData, jsonMemory, MAX_JSON_PROP);
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
    if (NULL != jsonMemory) {
        delete[] jsonMemory;
    }
    if (NULL != jsonCharData) {
        delete[] jsonCharData;
    }
    return initalId != *pIdCurrent;
}

VOID DeleteCmdDictBuffer(map<UINT, CHAR*>& mCmdDict) {
    for (pair<const UINT, CHAR*>& keyValuePair : mCmdDict) {
        delete[] keyValuePair.second;
        keyValuePair.second = nullptr;
    }
}
