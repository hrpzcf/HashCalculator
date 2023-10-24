// 菜单扩展实现方式参考：
// https://gitee.com/peterxiang/template_IContextMenuExt
// https://blog.csdn.net/u012741077/article/details/50642895

#include "pch.h"
#include <shlobj_core.h>
#include <strsafe.h>
#include "commons.h"
#include "ComputeHash.h"
#include "ResString.h"

CONST SIZE_T MAX_CMD_CHARS = 32767;
LPCWSTR EXECUTABLE = L"HashCalculator.exe";

constexpr auto IDM_COMPUTE_HASH = 0;
constexpr auto IDM_COMPUTE_XXHASH32 = 1;
constexpr auto IDM_COMPUTE_XXHASH64 = 2;
constexpr auto IDM_COMPUTE_XXHASH3 = 3;
constexpr auto IDM_COMPUTE_XXHASH128 = 4;
constexpr auto IDM_COMPUTE_MD5 = 5;
constexpr auto IDM_COMPUTE_CRC32 = 6;
constexpr auto IDM_COMPUTE_WHIRLPOOL = 7;
constexpr auto IDM_COMPUTE_SHA1 = 8;
constexpr auto IDM_COMPUTE_SHA224 = 9;
constexpr auto IDM_COMPUTE_SHA256 = 10;
constexpr auto IDM_COMPUTE_SHA384 = 11;
constexpr auto IDM_COMPUTE_SHA512 = 12;
constexpr auto IDM_COMPUTE_SHA3_224 = 13;
constexpr auto IDM_COMPUTE_SHA3_256 = 14;
constexpr auto IDM_COMPUTE_SHA3_384 = 15;
constexpr auto IDM_COMPUTE_SHA3_512 = 16;
constexpr auto IDM_COMPUTE_BLAKE2B = 17;
constexpr auto IDM_COMPUTE_BLAKE2BP = 18;
constexpr auto IDM_COMPUTE_BLAKE2S = 19;
constexpr auto IDM_COMPUTE_BLAKE2SP = 20;
constexpr auto IDM_COMPUTE_BLAKE3 = 21;
constexpr auto IDM_COMPUTE_STREEBOG_256 = 22;
constexpr auto IDM_SUBMENUS_PARENT = 23;

VOID CComputeHash::CreateGUIProcessComputeHash(LPCWSTR algo) {
	if (nullptr == this->executable_path) {
		CResStringW title = CResStringW(this->module_inst, IDS_TITLE_ERROR);
		CResStringW text = CResStringW(this->module_inst, IDS_NO_EXECUTABLE_PATH);
		MessageBoxW(nullptr, text.String(), title.String(), MB_TOPMOST | MB_ICONERROR);
		return;
	}
	wstring command_line = wstring(EXECUTABLE);
	command_line += L" compute";
	if (nullptr != algo) {
		command_line += wstring(L" --algo ") + algo;
	}
	SIZE_T cmd_characters = command_line.length() + 1;
	for (SIZE_T i = 0; i < this->filepath_list.size(); ++i) {
		if (this->filepath_list[i][this->filepath_list[i].length() - 1] == L'\\') {
			this->filepath_list[i] += L'\\';
		}
		wstring current_cmd = L" \"" + this->filepath_list[i] + L"\"";
		SIZE_T cmd_characters_temp = cmd_characters + current_cmd.length();
		if (cmd_characters_temp < MAX_CMD_CHARS)
		{
			command_line += current_cmd;
			cmd_characters = cmd_characters_temp;
		}
		else if (cmd_characters_temp > MAX_CMD_CHARS) {
			break;
		}
		else if (cmd_characters_temp == MAX_CMD_CHARS) {
			command_line += current_cmd;
			cmd_characters = cmd_characters_temp;
			break;
		}
	}
	LPWSTR commandline_buffer;
	try
	{
		commandline_buffer = new WCHAR[cmd_characters];
	}
	catch (const std::bad_alloc&)
	{
		return;
	}
	StringCchCopyW(commandline_buffer, cmd_characters, command_line.c_str());
	STARTUPINFO startup_info = { 0 };
	startup_info.cb = sizeof(startup_info);
	PROCESS_INFORMATION proc_info = { 0 };
	if (CreateProcessW(this->executable_path, commandline_buffer, NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS,
		NULL, NULL, &startup_info, &proc_info))
	{
		CloseHandle(proc_info.hThread);
		CloseHandle(proc_info.hProcess);
	}
	delete[] commandline_buffer;
}

CComputeHash::CComputeHash() {
	this->module_inst = _AtlBaseModule.GetModuleInstance();
	try
	{
		DWORD bufsize = MAX_PATH;
		LPWSTR  module_dirpath = new WCHAR[bufsize];
		while (true)
		{
			DWORD size = GetModuleFileNameW(module_inst, module_dirpath, bufsize);
			if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
				delete[]  module_dirpath;
				bufsize += MAX_PATH;
				module_dirpath = new WCHAR[bufsize];
				continue;
			}
			if (!PathRemoveFileSpecW(module_dirpath)) {
				break;
			}
			SIZE_T moduledir_chars = wcslen(module_dirpath);
			if (moduledir_chars == 0) {
				break;
			}
			SIZE_T exec_total_chars = moduledir_chars + 2 + wcslen(EXECUTABLE);
			this->executable_path = new WCHAR[exec_total_chars]();
			StringCchCatW(this->executable_path, exec_total_chars, module_dirpath);
			StringCchCatW(this->executable_path, exec_total_chars, L"\\");
			StringCchCatW(this->executable_path, exec_total_chars, EXECUTABLE);
			break;
		}
		delete[] module_dirpath;
	}
	catch (const std::bad_alloc&) {
	}
	this->bitmap_menu1 = LoadBitmapW(module_inst, MAKEINTRESOURCEW(IDB_BITMAP_MENU1));
	this->bitmap_menu2 = LoadBitmapW(module_inst, MAKEINTRESOURCEW(IDB_BITMAP_MENU2));
}

CComputeHash::~CComputeHash() {
	delete[] this->executable_path;
	DeleteObject(this->bitmap_menu1);
	DeleteObject(this->bitmap_menu2);
}

STDMETHODIMP CComputeHash::Initialize(
	PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID) {
	WCHAR filepath_buffer[MAX_PATH];
	this->filepath_list.clear();
	if (nullptr != pidlFolder) {
		if (SHGetPathFromIDListW(pidlFolder, filepath_buffer)) {
			this->filepath_list.push_back(filepath_buffer);
			return S_OK;
		}
		return E_INVALIDARG;
	}
	STGMEDIUM	stg = { TYMED_HGLOBAL };
	FORMATETC	fmt = {
		CF_HDROP,
		nullptr,
		DVASPECT_CONTENT,
		-1,
		TYMED_HGLOBAL };
	HDROP		drop_handle = nullptr;
	if (nullptr == pdtobj) {
		return E_INVALIDARG;
	}
	if (FAILED(pdtobj->GetData(&fmt, &stg)))
	{
		return E_INVALIDARG;
	}
	drop_handle = (HDROP)GlobalLock(stg.hGlobal);
	if (nullptr == drop_handle)
	{
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
	}
	UINT file_count = DragQueryFileW(drop_handle, INFINITE, nullptr, 0);
	if (0 == file_count)
	{
		GlobalUnlock(stg.hGlobal);
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
	}
	for (UINT index = 0; index < file_count; index++)
	{
		if (0 != DragQueryFileW(drop_handle, index, filepath_buffer, MAX_PATH))
		{
			this->filepath_list.push_back(filepath_buffer);
		}
	}
	GlobalUnlock(stg.hGlobal);
	ReleaseStgMedium(&stg);
	return S_OK;
}

STDMETHODIMP CComputeHash::QueryContextMenu(
	HMENU hmenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags) {
	if (uFlags & CMF_DEFAULTONLY)
	{
		return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, 0);
	}
	CResStringW resource = CResStringW(this->module_inst, IDS_MENU_COMPUTE);
	InsertMenuW(hmenu, indexMenu, MF_BYPOSITION | MF_STRING | MF_POPUP,
		idCmdFirst + IDM_COMPUTE_HASH, resource.String());
	if (this->bitmap_menu1 != nullptr) {
		SetMenuItemBitmaps(hmenu, indexMenu, MF_BYPOSITION, this->bitmap_menu1, this->bitmap_menu1);
	}
	HMENU submenu_handle = CreatePopupMenu();
	LONG flag = MF_STRING | MF_POPUP;
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXHASH32, L"XXH32");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXHASH64, L"XXH64");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXHASH3, L"XXH3");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_XXHASH128, L"XXH128");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_MD5, L"MD5");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_CRC32, L"CRC32");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_WHIRLPOOL, L"Whirlpool");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA1, L"SHA1");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA224, L"SHA224");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA256, L"SHA256");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA384, L"SHA384");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA512, L"SHA512");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_224, L"SHA3-224");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_256, L"SHA3-256");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_384, L"SHA3-384");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_SHA3_512, L"SHA3-512");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2B, L"BLAKE2b-512");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2BP, L"BLAKE2bp-512");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2S, L"BLAKE2s-256");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE2SP, L"BLAKE2sp-256");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_BLAKE3, L"BLAKE3-256");
	AppendMenuW(submenu_handle, flag, idCmdFirst + IDM_COMPUTE_STREEBOG_256, L"Streebog-256");
	// 方法退出后 compute_hash_res 会被析构，compute_hash_text 会被 delete
	// menu_info.dwTypeData = compute_hash_text 安全? InsertMenuItemW 是否复制数据?
	CResStringW compute_hash_res = CResStringW(this->module_inst, IDS_MENU_COMPUTE_HASH);
	LPWSTR compute_hash_text = compute_hash_res.String();
	MENUITEMINFOW menu_info = { 0 };
	menu_info.cbSize = sizeof(MENUITEMINFOW);
	menu_info.fMask = MIIM_ID | MIIM_SUBMENU | MIIM_TYPE;
	menu_info.fType = MFT_STRING;
	menu_info.wID = idCmdFirst + IDM_SUBMENUS_PARENT;
	menu_info.hSubMenu = submenu_handle;
	menu_info.dwTypeData = compute_hash_text;
	menu_info.cch = (UINT)wcslen(compute_hash_text);
	if (nullptr != this->bitmap_menu2) {
		menu_info.fMask |= MIIM_CHECKMARKS;
		menu_info.hbmpChecked = this->bitmap_menu2;
		menu_info.hbmpUnchecked = this->bitmap_menu2;
	}
	InsertMenuItemW(hmenu, indexMenu + 1, true, &menu_info);
	return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, IDM_SUBMENUS_PARENT + 1);
}

STDMETHODIMP CComputeHash::InvokeCommand(CMINVOKECOMMANDINFO* pici) {
	if (0 != HIWORD(pici->lpVerb))
	{
		return E_INVALIDARG;
	}
	LPCWSTR algo = nullptr;
	switch (LOWORD(pici->lpVerb))
	{
	case IDM_COMPUTE_XXHASH32:
		algo = L"XXHASH32";
		break;
	case IDM_COMPUTE_XXHASH64:
		algo = L"XXHASH64";
		break;
	case IDM_COMPUTE_XXHASH3:
		algo = L"XXHASH3";
		break;
	case IDM_COMPUTE_XXHASH128:
		algo = L"XXHASH128";
		break;
	case IDM_COMPUTE_MD5:
		algo = L"MD5";
		break;
	case IDM_COMPUTE_CRC32:
		algo = L"CRC32";
		break;
	case IDM_COMPUTE_WHIRLPOOL:
		algo = L"WHIRLPOOL";
		break;
	case IDM_COMPUTE_HASH:
		break;
	case IDM_COMPUTE_SHA1:
		algo = L"SHA1";
		break;
	case IDM_COMPUTE_SHA224:
		algo = L"SHA224";
		break;
	case IDM_COMPUTE_SHA256:
		algo = L"SHA256";
		break;
	case IDM_COMPUTE_SHA384:
		algo = L"SHA384";
		break;
	case IDM_COMPUTE_SHA512:
		algo = L"SHA512";
		break;
	case IDM_COMPUTE_SHA3_224:
		algo = L"SHA3_224";
		break;
	case IDM_COMPUTE_SHA3_256:
		algo = L"SHA3_256";
		break;
	case IDM_COMPUTE_SHA3_384:
		algo = L"SHA3_384";
		break;
	case IDM_COMPUTE_SHA3_512:
		algo = L"SHA3_512";
		break;
	case IDM_COMPUTE_BLAKE2B:
		algo = L"BLAKE2B_512";
		break;
	case IDM_COMPUTE_BLAKE2BP:
		algo = L"BLAKE2BP_512";
		break;
	case IDM_COMPUTE_BLAKE2S:
		algo = L"BLAKE2S_256";
		break;
	case IDM_COMPUTE_BLAKE2SP:
		algo = L"BLAKE2SP_256";
		break;
	case IDM_COMPUTE_BLAKE3:
		algo = L"BLAKE3_256";
		break;
	case IDM_COMPUTE_STREEBOG_256:
		algo = L"STREEBOG_256";
		break;
	default:
		return E_INVALIDARG;
	}
	this->CreateGUIProcessComputeHash(algo);
	return S_OK;
}

STDMETHODIMP CComputeHash::GetCommandString(
	UINT_PTR idCmd,
	UINT uType,
	UINT* pReserved,
	_Out_writes_bytes_((uType& GCS_UNICODE) ? (cchMax * sizeof(wchar_t)) : cchMax) _When_(!(uType& (GCS_VALIDATEA | GCS_VALIDATEW)), _Null_terminated_) CHAR* pszName,
	UINT cchMax) {
	return E_NOTIMPL;
}
