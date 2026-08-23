using Microsoft.Extensions.Logging;

namespace HashCalculator;

/// <summary>
/// 文件日志的目录、文件名等常量配置。
/// </summary>
internal static class FileLogOptions
{
    /// <summary>日志级别默认值。</summary>
    public const LogLevel DefaultLevel = LogLevel.None;

    /// <summary>日志目录名（位于配置目录下）。</summary>
    public const string LogDirectoryName = "Logs";

    /// <summary>日志文件名中日期部分的格式。</summary>
    public const string FileDateFormat = "yyyyMMdd";

    /// <summary>日志文件名模板，{0} 为日期。</summary>
    public const string FileNameTemplate = "Logging_{0}.log";
}
