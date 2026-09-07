// Shell 扩展与主程序之间的命名管道通信
// 协议定义与 HashCalculator 端 IPC/IPCMessageHeader.cs、IPCMessageKind.cs 严格对应，
// 修改任一端必须同步另一端，否则解析会错位
#pragma once

#include <string>
#include <vector>
#include <Windows.h>

// 与 HashCalculator 端 HandlerIdentity 对齐，只能追加新值不能改已有值
enum HandlerIdentity : DWORD {
    IPC_HID_UNKNOWN = 0,
    IPC_HID_PARSE_ARGUMENTS = 1,
    IPC_HID_ACTIVATE = 2,
    IPC_HID_NAVIGATE_TO = 3,
    IPC_HID_GET_MULTI_MODE = 4,
    IPC_HID_SET_MULTI_MODE = 5,
};

// 与 HashCalculator 端 HandlerStatus 对齐（响应头 Status 字段取值）
enum HandlerStatus : DWORD {
    IPC_STATUS_HANDLED = 0,
    IPC_STATUS_ERROR = 1,
    IPC_STATUS_BAD_PAYLOAD = 2,
    IPC_STATUS_BAD_COMMAND = 3,
};

// 请求头：与 HashCalculator 端 RequestHeader 对齐
#pragma pack(push, 1)
struct IPCRequestHeader {
    DWORD Version;        // = IPC_REQ_HEADER_VER
    DWORD Identity;       // HandlerIdentity 的值
    DWORD PayloadBytes;   // 紧随其后的 Payload 字节数，0 表示无 Payload
};

// 响应头：与 HashCalculator 端 ResponseHeader 对齐
struct IPCResponseHeader {
    DWORD Status;        // HandlerStatus 的值
    DWORD PayloadBytes;  // 紧随其后的响应数据段字节数，0 表示无
};
#pragma pack(pop)

const DWORD IPC_REQ_HEADER_VER = 1; // Header 的版本

// 管道目录与前缀。每个 HashCalculator 实例监听一条
// "HashCalculator.IPC.{PID}.{启动时间Ticks}" 的管道。
const WCHAR* const HC_PIPE_DIR = L"\\\\.\\pipe\\";
const WCHAR* const HC_PIPE_PREFIX = L"HashCalculator.IPC.";

// 一次跨进程处理尝试的最终结果
enum IPCTryHandleResult {
    IPC_HANDLE_DONE,        // 已通过管道交给运行中的实例，无需再启动进程
    IPC_HANDLE_FAILED,      // 无实例 / 多实例模式 / 通信失败，需回退本地启动（CreateProcessW）
};

// 尝试把一次"校验文件哈希"请求经管道交给最早运行的实例。
// checklistPath 为校验信息文件路径；algos 为算法字符串，可为空。
// 返回 IPC_HANDLE_DONE 表示已交给实例；否则返回 IPC_HANDLE_FAILED。
IPCTryHandleResult TryHandleVerifyViaPipe(const std::wstring& algos, const std::wstring& checklistPath);

// 尝试把一次"计算文件哈希"请求经管道交给最早运行的实例。
// paths 为待计算的文件/文件夹路径；algos 为算法字符串，可为空。
// 返回 IPC_HANDLE_DONE 表示已交给实例；否则返回 IPC_HANDLE_FAILED。
IPCTryHandleResult TryHandleComputeViaPipe(const std::wstring& algos, const std::vector<std::wstring>& paths);
