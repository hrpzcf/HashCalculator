using System;
using System.Globalization;
using System.Text;
using HashCalculator.ViewModels.Pages;
using Microsoft.Extensions.Logging;

namespace HashCalculator;

/// <summary>
/// 将日志条目格式化后交给 <see cref="FileLoggerProvider"/> 写入 Logs 目录下
/// 按天命名的文件。日志开关（<see cref="SettingsViewModel.IsApplicationLoggingEnabled"/>）
/// 和最低级别（<see cref="SettingsViewModel.ApplicationLoggingLevel"/>）在每次写日志时实时读取。
/// </summary>
internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(FileLoggerProvider provider, string categoryName)
    {
        this._provider = provider;
        this._categoryName = categoryName;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception exception, Func<TState, Exception, string> formatter)
    {
        try
        {
            if (!this.IsEnabled(logLevel))
            {
                return;
            }
            if (formatter is null)
            {
                return;
            }
            string message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }
            this._provider.WriteLine(this.Format(logLevel, message, exception));
        }
        catch
        {
            // 日志记录过程中的任何异常都不能影响主程序，静默忽略
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (Settings.Current.IsApplicationLoggingEnabled)
        {
            return logLevel >= Settings.Current.ApplicationLoggingLevel;
        }
        return false;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    private static string LogLevelName(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => "NONE",
        };
    }

    private string Format(LogLevel logLevel, string message, Exception exception)
    {
        string levelName = LogLevelName(logLevel);
        string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
            CultureInfo.InvariantCulture);
        StringBuilder loggingLines = new StringBuilder(256);
        loggingLines.Append(formattedTime).Append(" [").Append(levelName).Append("] ");
        loggingLines.Append(this._categoryName).Append(": ").Append(message);
        // 注意：LoggerExtensions 的 formatter 只返回格式化后的消息文本，
        // 并不会把异常信息拼进 message。因此异常需在此单独追加，否则异常类型、消息和堆栈会丢失。
        if (exception != null)
        {
            loggingLines.Append(Environment.NewLine).Append(exception).Append(Environment.NewLine);
        }
        return loggingLines.ToString();
    }
}
