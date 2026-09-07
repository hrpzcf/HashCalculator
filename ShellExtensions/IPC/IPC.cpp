// 实现 Shell 经命名管道把计算/校验请求交给 HashCalculator 运行中的实例，
// 如果没有实例在运行、目标实例处于多实例模式、或通信失败，均回退到本地启动。

#include "pch.h"
#include "IPC.hpp"
#include <cstdlib>
#include <cwchar>
#include <string>
#include <vector>
#include <Windows.h>

using std::vector;
using std::wstring;

namespace {

    // 参数数组编码为 UTF-16LE，参数之间以宽字符 '\0' 分隔，
    // 与 HashCalculator 端 IPCPayloadCodecs（UTF-16LE）一致
    vector<BYTE> EncodeArguments(const vector<wstring>& args) {
        wstring joined;
        for (size_t i = 0; i < args.size(); ++i) {
            if (i != 0) {
                joined.push_back(L'\0');
            }
            joined.append(args[i]);
        }
        // 范围构造：直接把 joined 整段 UTF-16LE 字节复制进目标 vector
        const wchar_t* begin = joined.data();
        return vector<BYTE>(reinterpret_cast<const BYTE*>(begin),
            reinterpret_cast<const BYTE*>(begin + joined.size()));
    }

    // 枚举管道目录，找到最早启动（StartTicks 最小）的实例，返回其完整管道路径。
    // 管道名格式：HashCalculator.IPC.{PID}.{StartTicks}
    BOOL FindOldestAliveInstance(wstring& out_pipe_name) {
        wstring search_path(HC_PIPE_DIR);
        search_path.append(HC_PIPE_PREFIX).push_back(L'*');
        WIN32_FIND_DATAW fd = {};
        HANDLE h_find = FindFirstFileW(search_path.c_str(), &fd);
        if (INVALID_HANDLE_VALUE == h_find) {
            return FALSE;
        }
        BOOL found = FALSE;
        ULONGLONG best_ticks = 0;
        do {
            // fd.cFileName 形如 HashCalculator.IPC.{PID}.{StartTicks}
            wstring name = fd.cFileName;
            size_t prefix_len = wcslen(HC_PIPE_PREFIX);
            if (name.size() <= prefix_len) {
                continue;
            }
            // {PID}.{StartTicks}
            wstring rest = name.substr(prefix_len);
            size_t dot = rest.rfind(L'.');
            if (dot == wstring::npos || dot == rest.size() - 1) {
                continue;
            }
            // PID 与 StartTicks 均应为纯数字
            wstring ticks_str = rest.substr(dot + 1);
            if (ticks_str.empty()) {
                continue;
            }
            wchar_t* end_ptr = nullptr;
            ULONGLONG ticks = wcstoull(ticks_str.c_str(), &end_ptr, 10);
            if (end_ptr == ticks_str.c_str() || *end_ptr != L'\0') {
                continue;
            }
            if (!found || ticks < best_ticks) {
                best_ticks = ticks;
                out_pipe_name = wstring(HC_PIPE_DIR) + name;
                found = TRUE;
            }
        } while (FindNextFileW(h_find, &fd));
        FindClose(h_find);
        return found;
    }

    // 发送一条请求（头 + 可选 payload）、读取响应（头 + 可选数据段）
    // 响应头后跟变长数据段，故分两次读，与 HashCalculator 端写响应的方式对称
    // 返回命令是否被接受（响应 Status == Ok）；out_resp 可选回传响应数据段
    BOOL SendRequest(const wstring& pipe_name, DWORD handler_kind, const BYTE* payload,
        DWORD payload_byte_length, vector<BYTE>* out_response) {

        HANDLE h_pipe = CreateFileW(pipe_name.c_str(), GENERIC_READ | GENERIC_WRITE,
            0, NULL, OPEN_EXISTING, 0, NULL);
        if (INVALID_HANDLE_VALUE == h_pipe) {
            return FALSE;
        }
        IPCRequestHeader header = {
            IPC_REQ_HEADER_VER,
            handler_kind,
            payload_byte_length,
        };
        BOOL send_req_result = FALSE;
        DWORD written = 0;
        IPCResponseHeader response = {};
        do {
            // 写请求头
            if (!(WriteFile(h_pipe, &header, sizeof(header), &written, NULL) &&
                written == sizeof(header))) {
                break;
            }
            // 写 payload（如有）
            if (payload_byte_length != 0 &&
                !(WriteFile(h_pipe, payload, payload_byte_length, &written, NULL) &&
                    written == payload_byte_length)) {
                break;
            }
            // 读响应头
            DWORD read_bytes = 0;
            if (!(ReadFile(h_pipe, &response, sizeof(response), &read_bytes, NULL) &&
                read_bytes == sizeof(response))) {
                break;
            }
            send_req_result = (response.Status == IPC_STATUS_HANDLED);
            // 读数据段（如有）
            if (out_response == nullptr || response.PayloadBytes <= 0) {
                break;
            }
            out_response->resize(response.PayloadBytes);
            DWORD data_read = 0;
            if (!(ReadFile(h_pipe, out_response->data(), response.PayloadBytes, &data_read, NULL) &&
                data_read == response.PayloadBytes)) {
                out_response->clear();
            }
        } while (0);
        CloseHandle(h_pipe);
        return send_req_result;
    }

    // 询问目标实例是否为多实例模式（发 GetAppMultiMode，读响应单字节 0/1）
    BOOL QueryIsMultiInstance(const wstring& pipe_name, BOOL& out_is_multi) {
        vector<BYTE> response;
        if (!SendRequest(pipe_name, IPC_HID_GET_MULTI_MODE, NULL, 0, &response)) {
            return FALSE;
        }
        if (response.size() != 1) {
            return FALSE;
        }
        out_is_multi = (response[0] != 0);
        return TRUE;
    }

    // 统一决策：找最早实例 → 问是否多实例 → 单实例则发 ParseArguments。
    // 任一环节失败（无实例/多实例/通信失败）都返回 NEED_LOCAL。
    IPCTryHandleResult TryParseArgumentsViaPipe(const vector<wstring>& args) {
        wstring pipe_name;
        if (!FindOldestAliveInstance(pipe_name)) {
            return IPC_HANDLE_FAILED;
        }
        BOOL is_multi = FALSE;
        if (!QueryIsMultiInstance(pipe_name, is_multi)) {
            return IPC_HANDLE_FAILED;
        }
        if (is_multi) {
            // 多实例模式：期望新开窗口，回退本地启动
            return IPC_HANDLE_FAILED;
        }
        vector<BYTE> payload = EncodeArguments(args);
        if (!SendRequest(pipe_name, IPC_HID_PARSE_ARGUMENTS, payload.data(),
            (DWORD)payload.size(), NULL)) {
            return IPC_HANDLE_FAILED;
        }
        return IPC_HANDLE_DONE;
    }

}  // namespace


IPCTryHandleResult TryHandleComputeViaPipe(const wstring& algos, const vector<wstring>& paths) {
    vector<wstring> args;
    args.push_back(L"compute");
    if (!algos.empty()) {
        args.push_back(L"--algo");
        args.push_back(algos);
    }
    for (const wstring& path : paths) {
        args.push_back(path);
    }
    return TryParseArgumentsViaPipe(args);
}


IPCTryHandleResult TryHandleVerifyViaPipe(const wstring& algos, const wstring& checklist_path) {
    vector<wstring> args;
    args.push_back(L"verify");
    if (!algos.empty()) {
        args.push_back(L"--algo");
        args.push_back(algos);
    }
    args.push_back(L"--list");
    args.push_back(checklist_path);
    return TryParseArgumentsViaPipe(args);
}
