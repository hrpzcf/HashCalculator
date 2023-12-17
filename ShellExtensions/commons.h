#pragma once

#include <map>

using std::map;

#define MENU_JSONNAME               "menus.json"
#define HC_EXECUTABLE               "HashCalculator.exe"
#define HCEXE_REGPATH               "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths"
#define MAXJSONPROP                 480
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
    const char* menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, MenuType_t menuType,
    UINT* idCur, map<UINT, char*>& idCmdMap, HBITMAP hBitMap);
VOID DeleteCmdDictBuffer(map<UINT, char*>& mCmdDict);
