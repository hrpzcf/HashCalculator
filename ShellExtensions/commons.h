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
    /// HashCalculator ��ϵͳ�Ҽ���չ�˵�����
    /// </summary>
typedef enum {
    /// <summary>
    /// Ĭ��ֵ
    /// </summary>
    MENUTYPE_UNKNOWN,

    /// <summary>
    /// �˵����ڼ����ļ���ϣֵ�˵�
    /// </summary>
    MENUTYPE_COMPUTE,

    /// <summary>
    /// �˵�����У���ļ���ϣֵ�˵�
    /// </summary>
    MENUTYPE_CHECKHASH,
} MenuType_t;

BOOL GetHashCalculatorPath(LPSTR* buffer, LPDWORD bufsize);
VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType);
BOOL InsertMenuFromJsonFile(
    const CHAR* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, MenuType_t menuType,
    UINT* idCur, map<UINT, CHAR*>& idCmdMap, HBITMAP hBitMap);
VOID DeleteCmdDictBuffer(map<UINT, CHAR*>& mCmdDict);
