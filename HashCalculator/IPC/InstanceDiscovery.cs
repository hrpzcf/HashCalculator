using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace HashCalculator.IPC;

/// <summary>
/// 实例发现：每个实例用「管道名前缀 + 进程 ID + 启动时间戳」作为自己的管道名，
/// 其他实例通过枚举管道目录即可得知有哪些实例存活及其启动先后，
/// 因此不需要额外的注册表、共享内存或本地文件。
/// </summary>
internal static class InstanceDiscovery
{
    private const string PipeDirectory = @"\\.\pipe\";

    private const string PipeNamePrefix = "HashCalculator.IPC.";

    private static readonly long StartTicks = DateTime.UtcNow.Ticks;

    public static string OwnPipeName { get; } =
        $"{PipeNamePrefix}{Environment.ProcessId}.{StartTicks}";

    /// <summary>枚举其他存活实例，排除自己</summary>
    public static InstanceEndpoint[] Discover()
    {
        List<InstanceEndpoint> endpoints = [];
        string[] pipePaths;
        try
        {
            pipePaths = Directory.GetFiles(PipeDirectory);
        }
        catch (Exception)
        {
            // 管道目录不可枚举时视为无其他实例，调用方自己处理参数
            return Array.Empty<InstanceEndpoint>();
        }
        foreach (string path in pipePaths)
        {
            if (TryParse(path, out InstanceEndpoint endpoint) &&
                endpoint.ProcessId != Environment.ProcessId)
            {
                endpoints.Add(endpoint);
            }
        }
        return endpoints.ToArray();
    }

    /// <summary>
    /// 尝试获取最早启动的存活实例，即单实例模式下的命令行参数转发目标。
    /// 没有其他实例时返回 false，此时调用方应自己处理参数并正常启动。
    /// </summary>
    public static bool TryGetOldestAlive(out InstanceEndpoint endpoint)
    {
        InstanceEndpoint oldest = null;
        foreach (InstanceEndpoint item in Discover())
        {
            if (oldest is null || item.StartTicks < oldest.StartTicks)
            {
                oldest = item;
            }
        }
        endpoint = oldest;
        return oldest is not null;
    }

    /// <summary>解析成功时 endpoint 必定不为 null</summary>
    private static bool TryParse(string pipePath, out InstanceEndpoint endpoint)
    {
        endpoint = default;
        if (pipePath.Length <= PipeDirectory.Length)
        {
            return false;
        }
        string name = pipePath[PipeDirectory.Length..];
        if (!name.StartsWith(PipeNamePrefix, StringComparison.Ordinal))
        {
            return false;
        }
        string rest = name[PipeNamePrefix.Length..];
        int separator = rest.IndexOf('.');
        if (separator <= 0 || separator == rest.Length - 1)
        {
            return false;
        }
        if (!int.TryParse(rest.AsSpan(0, separator), NumberStyles.None,
                CultureInfo.InvariantCulture, out int processId))
        {
            return false;
        }
        if (!long.TryParse(rest.AsSpan(separator + 1), NumberStyles.None,
                CultureInfo.InvariantCulture, out long startTicks))
        {
            return false;
        }
        endpoint = new InstanceEndpoint(name, processId, startTicks);
        return true;
    }
}
