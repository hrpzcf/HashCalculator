﻿#include "pch.h"
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
    MessageBoxA(nullptr, message.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
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

BOOL InsertMenuFromJsonFile(const char* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast,
    MenuType_t menuType, UINT* pIdCurrent, map<UINT, char*>& mCmdDict, HBITMAP bitMapHandle) {
    json_t* jsonMemory = NULL;
    CHAR* jsonCharData = NULL;
    UINT initalId = *pIdCurrent;
    DeleteCmdDictBuffer(mCmdDict);
    mCmdDict.clear();
    if (menuType != MENUTYPE_UNKNOWN) {
        FILE* jsonFile;
        errno_t error = fopen_s(&jsonFile, menuJson, "rb");
        if (error != 0 || NULL == jsonFile) {
            goto FinalizeAndReturn;
        }
        fseek(jsonFile, 0L, SEEK_END);
        SIZE_T fileLength = ftell(jsonFile);
        jsonCharData = new CHAR[fileLength + 1];
        rewind(jsonFile);
        SIZE_T readLength = fread(jsonCharData, 1, fileLength, jsonFile);
        fclose(jsonFile);
        if (readLength != fileLength) {
            goto FinalizeAndReturn;
        }
        jsonMemory = new json_t[MAXJSONPROP];
        const json_t* topMenuListJson = json_create(jsonCharData, jsonMemory, MAXJSONPROP);
        if (NULL == topMenuListJson || JSON_ARRAY != json_getType(topMenuListJson)) {
            goto FinalizeAndReturn;
        }
        const json_t* topMenuJson;
        for (topMenuJson = json_getChild(topMenuListJson); NULL != topMenuJson; topMenuJson = json_getSibling(topMenuJson)) {
            int64_t int64MenuType;
            if (!json_getPropValueByType(topMenuJson, "MenuType", JSON_INTEGER, &int64MenuType)
                || int64MenuType != (int64_t)menuType) {
                continue;
            }
            const char* topMenuTitle;
            if (!json_getPropValueByType(topMenuJson, "Title", JSON_TEXT, &topMenuTitle)) {
                continue;
            }
            bool flagHasSubmenus;
            if (!json_getPropValueByType(topMenuJson, "HasSubmenus", JSON_BOOLEAN, &flagHasSubmenus)) {
                continue;
            }
            if (flagHasSubmenus) {
                const json_t* submenuListJson = json_getProperty(topMenuJson, "Submenus");
                if (NULL == submenuListJson || JSON_ARRAY != json_getType(submenuListJson)) {
                    continue;
                }
                UINT appendedSubmenuCount = 0U;
                HMENU hSubmenuContainer = CreatePopupMenu();
                LONG flag = MF_STRING | MF_POPUP;
                const json_t* submenuJson;
                for (submenuJson = json_getChild(submenuListJson); NULL != submenuJson; submenuJson = json_getSibling(submenuJson)) {
                    const char* submenuTitle, * submenuAlgoTypeStr;
                    if (!json_getPropValueByType(submenuJson, "Title", JSON_TEXT, &submenuTitle) ||
                        !json_getPropValueByType(submenuJson, "AlgType", JSON_TEXT, &submenuAlgoTypeStr)) {
                        continue;
                    }
                    if (AppendMenuA(hSubmenuContainer, flag, idCmdFirst + *pIdCurrent, submenuTitle)) {
                        ++appendedSubmenuCount;
                        size_t bufferCharLength = strlen(submenuAlgoTypeStr) + 1;
                        char* submenuAlgoTypeBuffer = new char[bufferCharLength]();
                        StringCchCopyA(submenuAlgoTypeBuffer, bufferCharLength, submenuAlgoTypeStr);
                        mCmdDict.emplace(*pIdCurrent, submenuAlgoTypeBuffer);
                        *pIdCurrent = *pIdCurrent + 1;
                    }
                }
                if (0 != appendedSubmenuCount) {
                    UINT mainMenuTitleLength = (UINT)strlen(topMenuTitle);
                    char* mainMenuTitleBuffer = new char[mainMenuTitleLength + 1];
                    StringCbCopyA(mainMenuTitleBuffer, mainMenuTitleLength + 1, topMenuTitle);
                    MENUITEMINFOA menuItemInformationBasedOnSubmenuContainer = { 0 };
                    menuItemInformationBasedOnSubmenuContainer.cbSize = sizeof(MENUITEMINFOA);
                    menuItemInformationBasedOnSubmenuContainer.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_STRING | MIIM_CHECKMARKS;
                    menuItemInformationBasedOnSubmenuContainer.hbmpChecked = bitMapHandle;
                    menuItemInformationBasedOnSubmenuContainer.hbmpUnchecked = bitMapHandle;
                    menuItemInformationBasedOnSubmenuContainer.wID = idCmdFirst + *pIdCurrent;
                    menuItemInformationBasedOnSubmenuContainer.hSubMenu = hSubmenuContainer;
                    menuItemInformationBasedOnSubmenuContainer.dwTypeData = mainMenuTitleBuffer;
                    menuItemInformationBasedOnSubmenuContainer.cch = mainMenuTitleLength;
                    if (InsertMenuItemA(hMenu, indexMenu + 1, true, &menuItemInformationBasedOnSubmenuContainer)) {
                        *pIdCurrent = *pIdCurrent + 1;
                        goto FinalizeAndReturn;
                    }
                }
                DestroyMenu(hSubmenuContainer);
            }
            else {
                const char* algoTypeStr;
                if (json_getPropValueByType(topMenuJson, "AlgType", JSON_TEXT, &algoTypeStr)) {
                    if (InsertMenuA(hMenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP, idCmdFirst + *pIdCurrent, topMenuTitle)) {
                        SetMenuItemBitmaps(hMenu, indexMenu, MF_BYPOSITION, bitMapHandle, bitMapHandle);
                        size_t bufferCharLength = strlen(algoTypeStr) + 1;
                        char* algoTypeBuffer = new char[bufferCharLength]();
                        StringCchCopyA(algoTypeBuffer, bufferCharLength, algoTypeStr);
                        mCmdDict.emplace(*pIdCurrent, algoTypeBuffer);
                        *pIdCurrent = *pIdCurrent + 1;
                    }
                }
            }
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

VOID DeleteCmdDictBuffer(map<UINT, char*>& mCmdDict) {
    for (pair<const UINT, char*>& keyValuePair : mCmdDict) {
        delete[] keyValuePair.second;
        keyValuePair.second = nullptr;
    }
}
