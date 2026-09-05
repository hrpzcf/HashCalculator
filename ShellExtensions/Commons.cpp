#include "pch.h"
#include "commons.h"
#include "tiny-json.h"
#include <cstdint>
#include <cstdio>
#include <map>
#include <string>
#include <Windows.h>

using std::map;
using std::wstring;


/// <summary>
/// 从注册表读取 HashCalculator.exe 的路径，此路径在安装系统快捷菜单扩展时被写入注册表。
/// </summary>
wstring GetHashCalculatorPath() {
    wstring hashCalculatorPath;
    HKEY hKeyHcAppPath = nullptr;
    HKEY hKeyCurrentUser = nullptr;
    DWORD valueDataType = 0;
    // RegGetValueW 的容量与返回大小均以字节计，且 REG_SZ 的大小包含终止符
    DWORD cbCapacity = 0;
    if (ERROR_SUCCESS != RegOpenCurrentUser(KEY_READ, &hKeyCurrentUser) ||
        ERROR_SUCCESS != RegOpenKeyExW(
            hKeyCurrentUser, HCEXE_REGPATH, 0, KEY_READ, &hKeyHcAppPath)) {
        if (ERROR_SUCCESS != RegOpenKeyExW(
            HKEY_LOCAL_MACHINE, HCEXE_REGPATH, 0, KEY_READ, &hKeyHcAppPath)) {
            goto FinalizeAndReturn;
        }
    }
    // pvData 传入 NULL，仅查询所需字节数
    if (ERROR_SUCCESS != RegGetValueW(
        hKeyHcAppPath, nullptr, nullptr, RRF_RT_REG_SZ, &valueDataType, nullptr, &cbCapacity)
        || REG_SZ != valueDataType) {
        goto FinalizeAndReturn;
    }
    hashCalculatorPath.resize((SIZE_T)cbCapacity / sizeof(WCHAR));
    if (ERROR_SUCCESS != RegGetValueW(
        hKeyHcAppPath, nullptr, nullptr, RRF_RT_REG_SZ, &valueDataType, &hashCalculatorPath[0], &cbCapacity)
        || REG_SZ != valueDataType) {
        hashCalculatorPath.clear();
        goto FinalizeAndReturn;
    }
    // 去掉终止符，使 size 反映真实字符数
    hashCalculatorPath.resize((SIZE_T)cbCapacity / sizeof(WCHAR) - 1);
FinalizeAndReturn:
    if (nullptr != hKeyHcAppPath) {
        RegCloseKey(hKeyHcAppPath);
    }
    if (nullptr != hKeyCurrentUser) {
        RegCloseKey(hKeyCurrentUser);
    }
    return hashCalculatorPath;
}


/// <summary>
/// 读取模块内的字符串资源，失败时返回空串。
/// 向 LoadStringW 的 cchBufferMax 传入 0 时，它不复制字符串，而是把指向资源本身的只读
/// 指针写入 lpBuffer 位置，返回值则为字符数。此写法无需预估长度，彻底避免缓冲不足导致的截断。
/// 注意 1：资源字符串未必以 '\0' 结尾，故此处按返回的字符数构造，不依赖终止符。
/// 注意 2：该特性仅 W 版本 API 函数具备，LoadStringA 传入 0 会损坏内存，不可照搬。
/// </summary>
wstring LoadResString(HMODULE hModule, UINT resId) {
    wstring result;
    LPCWSTR resource_pointer = nullptr;
    int char_count = LoadStringW(hModule, resId, (LPWSTR)&resource_pointer, 0);
    if (0 < char_count && nullptr != resource_pointer) {
        result.assign(resource_pointer, (SIZE_T)char_count);
    }
    return result;
}


VOID ShowMessageType(HMODULE hModule, UINT titleID, UINT messageID, UINT uType) {
    wstring title = LoadResString(hModule, titleID);
    wstring message = LoadResString(hModule, messageID);
    MessageBoxW(nullptr, message.c_str(), title.c_str(), uType);
}


/// <summary>
/// 将 tiny-json 解析得到的窄字符串按指定代码页转换为宽字符串，转换失败时返回空串。
/// </summary>
static wstring MultiByteToWideString(const char* multiByteStr, UINT codePage) {
    wstring result;
    if (nullptr == multiByteStr) {
        return result;
    }
    // 首次调用取得所需的 WCHAR 数量，该数量包含终止符
    int requiredWchCount = MultiByteToWideChar(codePage, 0, multiByteStr, -1, nullptr, 0);
    if (0 >= requiredWchCount) {
        return result;
    }
    // wstring 自身维护终止符，故只需容纳 requiredWchCount - 1 个字符
    result.resize((SIZE_T)requiredWchCount - 1);
    if (0 == MultiByteToWideChar(codePage, 0, multiByteStr, -1, &result[0], requiredWchCount)) {
        result.clear();
    }
    return result;
}


static BOOL GetPropValueByType(const json_t* parent, const char* propName,
    jsonType_t jsonType, void* addr) {

    if (NULL == parent || NULL == propName) {
        return FALSE;
    }
    const json_t* prop_json = json_getProperty(parent, propName);
    if (NULL == prop_json || jsonType != json_getType(prop_json)) {
        return FALSE;
    }
    switch (jsonType) {
    case JSON_BOOLEAN:
    {
        bool* bool_addr = (bool*)addr;
        *bool_addr = json_getBoolean(prop_json);
        break;
    }
    case JSON_INTEGER:
    {
        int64_t* int64_addr = (int64_t*)addr;
        *int64_addr = json_getInteger(prop_json);
        break;
    }
    case JSON_TEXT:
    {
        const char** string_addr = (const char**)addr;
        *string_addr = json_getValue(prop_json);
        break;
    }
    default:
        return FALSE;
    }
    return TRUE;
}


BOOL InsertMenuFromJsonFile(const wstring& menuJson, HMENU hMenu,
    UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, MenuType_t menuType, UINT* pIdCurrent,
    map<UINT, wstring>& mIDCmdToAlgos, HBITMAP bitMapHandle) {

    // utf8_data 供 tiny-json 解析使用：该库为原地解析器，只接受单/多字节编码的缓冲区，
    // 喂给它 WCHAR[] 会因大量内嵌的 '\0' 而解析失败，故此处必须保持窄字符。
    // 必须用 UTF-8 而非 ANSI(CP_ACP)：UTF-8 的后续字节恒为 0x80~0xBF，绝不与 JSON 语法字符
    // （{ } [ ] " : , 及转义符 \）冲突；而 GBK 等双字节编码的第二字节可能为 0x5C，
    // 会被解析器误判为转义符，导致解析错位。
    LPSTR utf8_data = NULL;
    LPWSTR unicode_data = NULL;
    UINT inital_id = *pIdCurrent;
    UINT index_top_current = indexMenu;
    FILE* json_file = NULL;
    json_t* json_memory = NULL;
    // 旧菜单项在此统一丢弃：map 存的是 wstring，清空即自动释放，无需手工 delete[]
    mIDCmdToAlgos.clear();
    if (menuType == MENUTYPE_UNKNOWN) {
        return FALSE;
    }
    // 用 _wfopen_s 直接以宽字符路径打开，避免路径含非 ANSI 字符时打不开菜单配置文件
    errno_t error = _wfopen_s(&json_file, menuJson.c_str(), L"rb");
    if (error != 0 || NULL == json_file) {
        goto FinalizeAndReturn;
    }
    fseek(json_file, 0L, SEEK_END);
    SIZE_T file_size = ftell(json_file);
    if (file_size < 2 || file_size % sizeof(WCHAR) != 0) {
        goto FinalizeAndReturn;
    }
    // 验证 UTF-16LE 编码的文件头部 2 字节是否是 FFFE
    const SIZE_T head_count = 2;
    CHAR utf16le_head[head_count] = { 0 };
    rewind(json_file);
    if (head_count != fread(utf16le_head, sizeof(CHAR), head_count, json_file)) {
        goto FinalizeAndReturn;
    }
    // 在小端字节序下 FFFE 两个字节对应的数值是 0xFEFF
    if (0xFEFFu != *((WORD*)utf16le_head)) {
        goto FinalizeAndReturn;
    }
    SIZE_T wch_count = file_size / sizeof(WCHAR);
    // 此处给 unicode_data 分配空间时不用为字符串末尾的 L'\0' 而 wchCount + 1，
    // 因为 UTF-16LE 编码文件头有 2 字节的标识 FFFE，不读取这两个字节省下的空间刚好够 L'\0'
    unicode_data = new WCHAR[wch_count];
    unicode_data[wch_count - 1] = 0;
    SIZE_T expected_count = file_size - head_count;
    SIZE_T byte_count_read = fread(unicode_data, sizeof(CHAR), expected_count, json_file);
    if (byte_count_read != expected_count) {
        goto FinalizeAndReturn;
    }
    INT req_size = WideCharToMultiByte(CP_UTF8, 0, unicode_data, -1, NULL, 0, NULL, NULL);
    if (0 == req_size) {
        goto FinalizeAndReturn;
    }
    utf8_data = new CHAR[req_size]();
    utf8_data[req_size - 1] = 0;
    if (0 == WideCharToMultiByte(CP_UTF8, 0, unicode_data, -1, utf8_data, req_size, NULL, NULL)) {
        goto FinalizeAndReturn;
    }
    json_memory = new json_t[MAX_JSON_PROP];
    const json_t* top_list_json = json_create(utf8_data, json_memory, MAX_JSON_PROP);
    if (NULL == top_list_json || JSON_ARRAY != json_getType(top_list_json)) {
        goto FinalizeAndReturn;
    }
    const json_t* top_menu_json = NULL;
    for (top_menu_json = json_getChild(top_list_json);
        top_menu_json;
        top_menu_json = json_getSibling(top_menu_json)) {
        int64_t i64_menu_type;
        const char* top_menu_title;
        if (!GetPropValueByType(top_menu_json, JSON_MENUTYPE, JSON_INTEGER, &i64_menu_type)
            || i64_menu_type != (int64_t)menuType
            || !GetPropValueByType(top_menu_json, JSON_TITLE, JSON_TEXT, &top_menu_title)) {
            continue;
        }
        // InsertMenuItemW/AppendMenuW 会自行复制标题字符串，故转换结果用局部 wstring 保存即可
        wstring top_menu_title_wide = MultiByteToWideString(top_menu_title, CP_UTF8);
        MENUITEMINFOW top_menu_submenu_info = { 0 };
        top_menu_submenu_info.fMask = MIIM_ID | MIIM_STRING | MIIM_BITMAP;
        top_menu_submenu_info.cbSize = sizeof(top_menu_submenu_info);
        top_menu_submenu_info.wID = idCmdFirst + *pIdCurrent;
        top_menu_submenu_info.cch = (UINT)top_menu_title_wide.length();
        top_menu_submenu_info.dwTypeData = &top_menu_title_wide[0];
        top_menu_submenu_info.hbmpItem = bitMapHandle;
        const json_t* sub_list_json = json_getProperty(top_menu_json, JSON_SUBMENUS);
        if (NULL == sub_list_json) {
            const char* algorithms_str;
            if (GetPropValueByType(top_menu_json, JSON_ALGTYPES, JSON_TEXT, &algorithms_str) &&
                InsertMenuItemW(hMenu, index_top_current, true, &top_menu_submenu_info)) {
                ++index_top_current;
                mIDCmdToAlgos.emplace(*pIdCurrent, MultiByteToWideString(algorithms_str, CP_UTF8));
                *pIdCurrent = *pIdCurrent + 1;
            }
        }
        else if (JSON_ARRAY == json_getType(sub_list_json)) {
            HMENU h_submenu_container = CreatePopupMenu();
            top_menu_submenu_info.hSubMenu = h_submenu_container;
            top_menu_submenu_info.fMask |= MIIM_SUBMENU;
            UINT appended_submenu_count = 0U;
            LONG flag = MF_STRING | MF_POPUP;
            const json_t* submenu_json = NULL;
            for (submenu_json = json_getChild(sub_list_json);
                submenu_json;
                submenu_json = json_getSibling(submenu_json)) {
                const char* submenu_title, * submenu_algos_str;
                if (GetPropValueByType(submenu_json, JSON_TITLE, JSON_TEXT, &submenu_title)
                    && GetPropValueByType(submenu_json, JSON_ALGTYPES, JSON_TEXT, &submenu_algos_str)) {
                    wstring submenu_title_wide = MultiByteToWideString(submenu_title, CP_UTF8);
                    if (AppendMenuW(h_submenu_container, flag, idCmdFirst + *pIdCurrent, submenu_title_wide.c_str())) {
                        ++appended_submenu_count;
                        mIDCmdToAlgos.emplace(*pIdCurrent, MultiByteToWideString(submenu_algos_str, CP_UTF8));
                        *pIdCurrent = *pIdCurrent + 1;
                    }
                }
            }
            if (0 != appended_submenu_count &&
                InsertMenuItemW(hMenu, index_top_current, true, &top_menu_submenu_info)) {
                ++index_top_current;
                *pIdCurrent = *pIdCurrent + 1;
                continue;
            }
            DestroyMenu(h_submenu_container);
        }
    }
FinalizeAndReturn:
    if (NULL != json_file) {
        fclose(json_file);
    }
    if (NULL != json_memory) {
        delete[] json_memory;
    }
    if (NULL != utf8_data) {
        delete[] utf8_data;
    }
    if (NULL != unicode_data) {
        delete[] unicode_data;
    }
    return inital_id != *pIdCurrent;
}
