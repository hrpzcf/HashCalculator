using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace HashCalculator;

/// <summary>
/// 将日志写入 <see cref="Settings.ConfigInfo.ActiveConfigDir"/> 下 Logs 目录的
/// ILoggerProvider 实现。每次写日志时都会重新读取
/// <see cref="Settings.Current"/> 的开关和级别，因此设置调整可以立即生效。
/// </summary>
/// <remarks>
/// 文件写入统一由本 provider（DI 中的单例）管理，持有唯一的 <see cref="StreamWriter"/>，
/// 避免多个 FileLogger 实例各自打开同一日志文件导致占用冲突。
/// </remarks>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly Lock _writeLock = new Lock();
    private readonly ReaderWriterLockSlim _dirLock = new();
    private string _currentDate = null;
    private string _logDirectory = null;
    private StreamWriter _streamWriter = null;

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(this, categoryName);
    }

    /// <summary>
    /// 追加一行日志文本到当日日志文件。线程安全。
    /// </summary>
    internal void WriteLine(string line)
    {
        lock (this._writeLock)
        {
            try
            {
                StreamWriter writer = this.GetStreamWriter();
                if (writer == null)
                {
                    return;
                }
                writer.WriteLine(line);
                writer.Flush();
            }
            catch
            {
                // 日志写入失败不能影响主程序运行，静默忽略
            }
        }
    }

    /// <summary>
    /// 获取日志目录，并确保目录存在（按需延迟创建）。
    /// </summary>
    private string GetLogDirectory()
    {
        this._dirLock.EnterReadLock();
        try
        {
            if (this._logDirectory != null)
            {
                return this._logDirectory;
            }
        }
        finally
        {
            this._dirLock.ExitReadLock();
        }
        this._dirLock.EnterWriteLock();
        try
        {
            if (this._logDirectory == null)
            {
                string logDir = Path.Combine(Settings.ConfigInfo.ActiveConfigDir,
                    FileLogOptions.LogDirectoryName);
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch
                {
                    // 目录创建失败（权限等）时退回 exe 所在目录，保证日志仍可写入
                    logDir = Path.Combine(
                        ConfigPaths.ConfigDirExec, FileLogOptions.LogDirectoryName);
                    Directory.CreateDirectory(logDir);
                }
                this._logDirectory = logDir;
            }
            return this._logDirectory;
        }
        finally
        {
            this._dirLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 获取（必要时创建）当日日志文件的写入器。日期变化时重建文件流。
    /// </summary>
    private StreamWriter GetStreamWriter()
    {
        string today = DateTime.Now.ToString(FileLogOptions.FileDateFormat,
            CultureInfo.InvariantCulture);
        if (this._streamWriter != null && this._currentDate == today)
        {
            return this._streamWriter;
        }
        // 日期变化或首次写入，重建文件流
        this._streamWriter?.Dispose();
        this._currentDate = today;
        string filePath = Path.Combine(this.GetLogDirectory(),
            string.Format(FileLogOptions.FileNameTemplate, today));
        this._streamWriter = new StreamWriter(filePath, append: true, Encoding.UTF8,
            bufferSize: 4096);
        return this._streamWriter;
    }

    public void Dispose()
    {
        lock (this._writeLock)
        {
            this._streamWriter?.Dispose();
            this._streamWriter = null;
        }
        this._dirLock.Dispose();
    }
}
