#pragma once

#include <map>

using std::map;

#define MENU_JSONNAME               "menus.json"
#define HC_EXECUTABLE               "HashCalculator.exe"
#define HCEXE_REGPATH               "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths"
#define MAXJSONPROP                 480
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
    const char* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, MenuType_t menuType,
    UINT* idCur, map<UINT, char*>& idCmdMap, HBITMAP hBitMap);
VOID DeleteCmdDictBuffer(map<UINT, char*>& mCmdDict);
