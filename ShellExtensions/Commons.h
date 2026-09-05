#pragma once

#include <map>
#include <string>
#include <Windows.h>

using std::map;
using std::wstring;

// JSON_* 系列宏供 tiny-json 使用，该库为原地解析器，只接受单/多字节编码的缓冲区，
// 喂给它 WCHAR[] 会因大量内嵌的 '\0' 而解析失败，故这些宏必须保持窄字符。
#define JSON_MENUTYPE               "MenuType"
#define JSON_TITLE                  "Title"
#define JSON_ALGTYPES               "AlgTypes"
#define JSON_SUBMENUS               "Submenus"
#define MENU_JSONNAME               L"menus_unicode.json"
#define HC_EXECUTABLE               L"HashCalculator.exe"
#define HCEXE_REGPATH               L"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\HashCalculator.exe"
#define MAX_JSON_PROP               512

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

/// <summary>
/// 从注册表读取 HashCalculator.exe 的路径，此路径在安装系统快捷菜单扩展时被写入注册表。
/// 先查 HKCU，不存在时回退到 HKLM。
/// </summary>
/// <returns>可执行文件完整路径；读取失败、键值不存在或类型不符时返回空字符串。</returns>
wstring GetHashCalculatorPath();
/// <summary>
/// 读取模块内的字符串资源，失败时返回空串，长度由资源实际内容决定，无截断风险。
/// </summary>
wstring LoadResString(HMODULE hModule, UINT resId);
VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType);
BOOL InsertMenuFromJsonFile(const wstring& menuJson, HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast,
    MenuType_t menuType, UINT* idCur, map<UINT, wstring>& idCmdMap, HBITMAP hBitMap);
