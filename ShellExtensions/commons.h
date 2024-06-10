#pragma once

#include <map>

using std::map;

#define JSON_MENUTYPE               "MenuType"
#define JSON_TITLE                  "Title"
#define JSON_ALGTYPES               "AlgTypes"
#define JSON_SUBMENUS               "Submenus"
#define MENU_JSONNAME               "menus_unicode.json"
#define HC_EXECUTABLE               "HashCalculator.exe"
#define HCEXE_REGPATH               "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\HashCalculator.exe"
#define MAX_JSON_PROP               480
#define MAX_CMD_CHARS               32767

/// <summary>
    /// HashCalculator 的系统右键扩展菜单类型
    /// </summary>
typedef enum {
    /// <summary>
    /// 默认值
    /// </summary>
    MENUTYPE_UNKNOWN,

    /// <summary>
    /// 菜单属于计算文件哈希值菜单
    /// </summary>
    MENUTYPE_COMPUTE,

    /// <summary>
    /// 菜单属于校验文件哈希值菜单
    /// </summary>
    MENUTYPE_CHECKHASH,
} MenuType_t;

BOOL GetHashCalculatorPath(LPSTR* buffer, LPDWORD bufsize);
VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType);
BOOL InsertMenuFromJsonFile(
    const CHAR* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, MenuType_t menuType,
    UINT* idCur, map<UINT, CHAR*>& idCmdMap, HBITMAP hBitMap);
VOID DeleteCmdDictBuffer(map<UINT, CHAR*>& mCmdDict);
