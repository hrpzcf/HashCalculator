using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace HashCalculator;

/// <summary>
/// 大文件分块读取器：用 FILE_FLAG_NO_BUFFERING 绕开系统文件缓存（省去内核到用户的一次拷贝），
/// 用 FILE_FLAG_OVERLAPPED 保持多块在途异步读，使调用方计算当前块时磁盘已经在读后续几块。<br/>
/// 实测 8 GB 文件：普通 FileStream 同步读算约 7.7 s，本读取器约 3.8 s。
/// </summary>
/// <remarks>
/// 非线程安全：<see cref="TryReadNextBlock"/> 只能由同一个线程依次调用。<br/>
/// 取到一块数据后必须先把它处理完，再进行下一次调用：槽按 SlotCount 个循环复用，
/// 下一次提交会覆盖最早那个槽的数据。
/// </remarks>
internal sealed class SequentialBlockReader : IDisposable
{
    /// <summary>
    /// NO_BUFFERING 的对齐单位。设备物理扇区常见 512 或 4096，少数为 8192、16384，
    /// 取 64 KB 是它们的公倍数，可一次覆盖，代价只是每个槽多预留 64 KB。
    /// </summary>
    private const int AlignmentSize = 64 * 1024;

    /// <summary>
    /// 默认块大小：对齐单位的 64 倍（4 MiB）。NO_BUFFERING 要求读取长度与文件偏移都
    /// 按设备扇区对齐，由对齐单位推导得出可保证它始终是整数倍。
    /// </summary>
    public const int DefaultBlockSize = AlignmentSize * 64;

    /// <summary>
    /// 槽位数（= 同时在途的异步读块数）。在途 3 块共 12 MiB：实测请求越大设备效率越高，
    /// 故块取 4 MiB；槽数取 4 是为了让在途数据量不至于过大（内存 4 × (4 MiB + 64 KiB) ≈ 16.25 MiB）。
    /// </summary>
    private const int SlotCount = 4;
    private const uint Infinite = 0xFFFFFFFF;

    /// <summary>
    /// 取消在途读后等待其结束的总预算，正常情况下取消完成是毫秒级，
    /// 这里只是极端情况下避免 Dispose 无限阻塞的兜底。
    /// </summary>
    private const uint DisposeWaitTimeout = 1000;

    /// <summary>
    /// 单块等待读取的耗时超过该毫秒数即记一条 Debug 日志，用于区分"读磁盘慢"与"算得慢"：
    /// 计算总耗时里若出现多次这种尖峰，说明瓶颈在磁盘读取的瞬时延迟，而不是算法本身。
    /// </summary>
    private const double SlowReadThresholdMs = 5;

    private static readonly int OverlappedSize = Marshal.SizeOf<OVERLAPPED>();

    private readonly int _blockSize;
    private readonly int[] _slotOffsets;
    private readonly long _fileLength;
    private readonly long _alignedLength;
    private readonly byte[] _buffer;
    private readonly IntPtr _handle;
    private readonly IntPtr _overlapped;
    private readonly IntPtr[] _completionEvents;
    private readonly IntPtr _tailHandle;
    private readonly IntPtr[] _slotPointers;

    private bool _disposed;
    private bool _tailDone;
    private long _consumed;
    private long _submitted;

    private SequentialBlockReader(IntPtr handle, IntPtr tailHandle, IntPtr[] events,
        long fileLength, int blockSize)
    {
        this._handle = handle;
        this._tailHandle = tailHandle;
        this._completionEvents = events;
        this._fileLength = fileLength;
        this._blockSize = blockSize;
        this._alignedLength = fileLength - (fileLength % AlignmentSize);
        // 构造中途失败时对象拿不到 Dispose，这里自己归还已分配的本机内存
        IntPtr overlapped = IntPtr.Zero;
        try
        {
            // 每次 Submit 都会完整重写目标 OVERLAPPED 的全部字段，故无需预先清零
            overlapped = Marshal.AllocHGlobal(OverlappedSize * SlotCount);
            // 两个槽共用一个数组：每个槽先预留一个对齐单位的余量，再在对齐后的地址上交给 ReadFile。
            // pinned: true 会把数组放进 POH（地址固定不移），因此无需再 GCHandle 固定。
            byte[] buffer = GC.AllocateUninitializedArray<byte>(
                SlotCount * (blockSize + AlignmentSize), pinned: true);
            IntPtr baseAddress = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
            int[] slotOffsets = new int[SlotCount];
            IntPtr[] slotPointers = new IntPtr[SlotCount];
            for (int i = 0; i < SlotCount; ++i)
            {
                long slotStart = (long)baseAddress + (i * (blockSize + AlignmentSize));
                long aligned = (slotStart + AlignmentSize - 1) & ~(AlignmentSize - 1);
                slotOffsets[i] = (int)(aligned - baseAddress);
                slotPointers[i] = new IntPtr(aligned);
            }
            this._overlapped = overlapped;
            this._buffer = buffer;
            this._slotOffsets = slotOffsets;
            this._slotPointers = slotPointers;
        }
        catch
        {
            if (overlapped != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(overlapped);
            }
            throw;
        }
    }

    /// <summary>
    /// 块数据所在的数组，配合 out 的 offset/length 传给 TransformBlock。
    /// </summary>
    public byte[] Buffer => this._buffer;

    /// <summary>
    /// 尝试创建。文件小到一次就读得完（重叠读取没有意义）、或存储/文件系统不接受
    /// NO_BUFFERING（含首块预读探测失败）时返回 null，调用方应回退到普通 FileStream 读取。
    /// </summary>
    public static SequentialBlockReader TryCreate(string path, long fileLength, int blockSize,
        out string failure)
    {
        failure = null;
        blockSize -= blockSize % AlignmentSize;
        if (blockSize <= 0 || fileLength <= blockSize)
        {
            failure = $"文件 {fileLength} 字节不超过块大小 {blockSize}";
            return null;
        }
        IntPtr handle = HANDLE.INVALID_HANDLE_VALUE;
        IntPtr tailHandle = IntPtr.Zero;
        IntPtr[] events = new IntPtr[SlotCount];
        bool created = false;
        try
        {
            handle = KERNEL32.CreateFileWithFlags(
                path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_NO_BUFFERING |
                FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED |
                FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_SEQUENTIAL_SCAN,
                IntPtr.Zero);
            if (handle == HANDLE.INVALID_HANDLE_VALUE)
            {
                failure = $"CreateFileW 失败，错误码 {Marshal.GetLastWin32Error()}";
                return null;
            }
            for (int i = 0; i < SlotCount; ++i)
            {
                // 手动复位事件：每个槽的完成通知不与其他在途读混淆
                events[i] = KERNEL32.CreateEventW(IntPtr.Zero, true, false, null);
                if (events[i] == IntPtr.Zero)
                {
                    failure = $"CreateEventW 失败，错误码 {Marshal.GetLastWin32Error()}";
                    return null;
                }
            }
            if (fileLength % AlignmentSize != 0)
            {
                // NO_BUFFERING 只接受对齐的长度，末尾不足一个对齐单位的字节另用带缓存的句柄读
                tailHandle = KERNEL32.CreateFileWithFlags(
                    path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open,
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero);
                if (tailHandle == HANDLE.INVALID_HANDLE_VALUE)
                {
                    // 尾部读不了就整条回退，免得算出一个缺尾巴的哈希
                    int error = Marshal.GetLastWin32Error();
                    tailHandle = IntPtr.Zero;
                    failure = $"打开尾部读取句柄失败，错误码 {error}";
                    return null;
                }
            }
            SequentialBlockReader reader = new SequentialBlockReader(handle, tailHandle, events,
                fileLength, blockSize);
            // 句柄与事件已交给读取器，下面 finally 不能再重复关闭
            handle = HANDLE.INVALID_HANDLE_VALUE;
            tailHandle = IntPtr.Zero;
            events = new IntPtr[SlotCount];
            created = true;
            // 预读首块：对齐方式或读取参数不被设备接受（如物理扇区更大）时
            // 只有真正 ReadFile 才会暴露，这里提前探测，失败就让调用方回退到带缓存的读取
            if (!reader.TryPrefetchFirstBlock(out string prefetchFailure))
            {
                reader.Dispose();
                failure = prefetchFailure;
                return null;
            }
            return reader;
        }
        catch (Exception e)
        {
            failure = "创建读取器异常：" + e.Message;
            return null;
        }
        finally
        {
            // 只在创建失败时归还，成功时这些句柄由读取器持有到 Dispose
            if (!created)
            {
                if (handle != IntPtr.Zero && handle != HANDLE.INVALID_HANDLE_VALUE)
                {
                    KERNEL32.CloseHandle(handle);
                }
                if (tailHandle != IntPtr.Zero && tailHandle != HANDLE.INVALID_HANDLE_VALUE)
                {
                    KERNEL32.CloseHandle(tailHandle);
                }
                foreach (IntPtr completionEvent in events)
                {
                    if (completionEvent != IntPtr.Zero)
                    {
                        KERNEL32.CloseHandle(completionEvent);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 取下一块。返回 true 时 offset/length 描述本块在 <see cref="Buffer"/> 中的位置。<br/>
    /// 返回前会先把后续块提交为异步读，因此调用方紧接着的哈希计算与磁盘读取是重叠的。
    /// </summary>
    public bool TryReadNextBlock(out int offset, out int length)
    {
        offset = 0;
        length = 0;
        while (this._submitted - this._consumed < SlotCount
            && (this._submitted * this._blockSize) < this._alignedLength)
        {
            this.Submit(this._submitted);
            ++this._submitted;
        }
        if (this._consumed >= this._submitted)
        {
            return this.TryReadTail(out offset, out length);
        }
        int slot = (int)(this._consumed % SlotCount);
        // 只给等待本身计时：尖峰才记日志，正常路径仅多两次高精度时间戳读取
        long waitStart = Stopwatch.GetTimestamp();
        uint waitResult = KERNEL32.WaitForSingleObject(this._completionEvents[slot], Infinite);
        if (waitResult != 0)
        {
            throw new IOException("等待读取完成失败", new Win32Exception(Marshal.GetLastWin32Error()));
        }
        double waited = Stopwatch.GetElapsedTime(waitStart).TotalMilliseconds;
        if (waited >= SlowReadThresholdMs &&
            App.Logger is { } logger && logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("块读取等待尖峰：第 {Block} 块，偏移 {Position} 字节，等待 {Waited:F1} 毫秒",
                this._consumed, this._consumed * this._blockSize, waited);
        }
        IntPtr overlapped = IntPtr.Add(this._overlapped, slot * OverlappedSize);
        // 错误码记在 OVERLAPPED.Internal（NTSTATUS），负值表示这次读失败；
        // 不区分它与"读到 0 字节"会把设备错误当成文件结束，算出一个不报错的错误哈希
        int status = Marshal.ReadInt32(overlapped);
        if (status < 0)
        {
            throw new IOException($"读取文件失败，NTSTATUS 0x{status:X8}");
        }
        long transferred = ReadTransferred(overlapped);
        if (transferred <= 0)
        {
            // 提交长度都是按对齐长度算好的，正常不会读到 0 字节，出现即说明文件在计算期间变短
            throw new IOException("读取文件时意外到达文件末尾");
        }
        length = (int)transferred;
        offset = this._slotOffsets[slot];
        ++this._consumed;
        return true;
    }

    /// <summary>
    /// 重叠 I/O 完成后的传输字节数记在 OVERLAPPED.InternalHigh，宽度与指针相同。
    /// </summary>
    private static long ReadTransferred(IntPtr overlapped)
    {
        IntPtr information = IntPtr.Add(overlapped, IntPtr.Size);
        return IntPtr.Size == 8 ? Marshal.ReadInt64(information) : (uint)Marshal.ReadInt32(information);
    }

    /// <summary>
    /// 预读第一块并确认设备接受当前的对齐方式与读取参数。<br/>
    /// 预读的这块不会被丢弃：事件是手动复位的，信号保持到下次提交前，
    /// 后续 <see cref="TryReadNextBlock"/> 直接取走即可。
    /// </summary>
    private bool TryPrefetchFirstBlock(out string failure)
    {
        failure = null;
        try
        {
            this.Submit(0);
            this._submitted = 1;
            if (KERNEL32.WaitForSingleObject(this._completionEvents[0], Infinite) != 0)
            {
                failure = $"预读首块时等待读取完成失败，错误码：{Marshal.GetLastWin32Error()}";
                return false;
            }
            int status = Marshal.ReadInt32(this._overlapped);
            if (status < 0)
            {
                failure = $"预读首块失败，NTSTATUS 0x{status:X8}";
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            failure = "预读首块异常：" + e.Message;
            return false;
        }
    }

    /// <summary>
    /// 提交第 blockIndex 块为异步读。<br/>
    /// 抛异常时本读取器的状态已不可信（在途计数与槽占用不再对应），调用方应直接放弃本轮计算。
    /// </summary>
    private void Submit(long blockIndex)
    {
        int slot = (int)(blockIndex % SlotCount);
        long position = blockIndex * this._blockSize;
        long remaining = this._alignedLength - position;
        int length = (int)(remaining < this._blockSize ? remaining : this._blockSize);
        IntPtr overlapped = IntPtr.Add(this._overlapped, slot * OverlappedSize);
        IntPtr completionEvent = this._completionEvents[slot];
        if (!KERNEL32.ResetEvent(completionEvent))
        {
            throw new IOException("复位读取完成事件失败", new Win32Exception(Marshal.GetLastWin32Error()));
        }
        Marshal.StructureToPtr(new OVERLAPPED
        {
            Offset = (uint)position,
            OffsetHigh = (uint)(position >> 32),
            hEvent = completionEvent
        }, overlapped, false);
        if (!KERNEL32.ReadFileAsync(this._handle, this._slotPointers[slot], (uint)length, IntPtr.Zero, overlapped))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != WIN32_ERROR.ERROR_IO_PENDING)
            {
                throw new IOException("提交读取请求失败", new Win32Exception(error));
            }
        }
    }

    private bool TryReadTail(out int offset, out int length)
    {
        offset = 0;
        length = 0;
        if (this._tailDone || this._tailHandle == IntPtr.Zero)
        {
            return false;
        }
        this._tailDone = true;
        // 此时所有提交的块都已取走，槽 0 空闲；普通（带缓存）读不要求地址对齐
        int remaining = (int)(this._fileLength - this._alignedLength);
        if (!KERNEL32.SetFilePointerEx(this._tailHandle, this._alignedLength, IntPtr.Zero, 0))
        {
            throw new IOException("定位到文件尾部失败", new Win32Exception(Marshal.GetLastWin32Error()));
        }
        IntPtr target = this._slotPointers[0];
        int total = 0;
        while (total < remaining)
        {
            if (!KERNEL32.ReadFileSync(this._tailHandle, IntPtr.Add(target, total),
                (uint)(remaining - total), out uint got, IntPtr.Zero))
            {
                throw new IOException("读取文件尾部失败", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            if (got == 0)
            {
                // 正常文件不会在这里读到 0 字节，出现即说明文件在计算期间变短
                throw new IOException("读取文件尾部时意外到达文件末尾");
            }
            total += (int)got;
        }
        offset = this._slotOffsets[0];
        length = total;
        return true;
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }
        this._disposed = true;
        if (this._handle != IntPtr.Zero && this._handle != HANDLE.INVALID_HANDLE_VALUE)
        {
            // CancelIoEx 是异步取消：必须等这些读真正完成（完成通知照常触发）后才能释放
            // OVERLAPPED 内存与缓冲，否则内核可能仍在写已释放的内存。
            // 所有在途槽共享一个等待预算，避免多个槽各自超时导致 Dispose 卡上数倍的超时时间
            _ = KERNEL32.CancelIoEx(this._handle, IntPtr.Zero);
            long deadline = Environment.TickCount64 + DisposeWaitTimeout;
            for (long i = this._consumed; i < this._submitted; ++i)
            {
                long remaining = deadline - Environment.TickCount64;
                if (remaining <= 0)
                {
                    break;
                }
                int slot = (int)(i % SlotCount);
                _ = KERNEL32.WaitForSingleObject(this._completionEvents[slot], (uint)remaining);
            }
            KERNEL32.CloseHandle(this._handle);
        }
        if (this._tailHandle != IntPtr.Zero && this._tailHandle != HANDLE.INVALID_HANDLE_VALUE)
        {
            KERNEL32.CloseHandle(this._tailHandle);
        }
        foreach (IntPtr completionEvent in this._completionEvents)
        {
            if (completionEvent != IntPtr.Zero)
            {
                KERNEL32.CloseHandle(completionEvent);
            }
        }
        if (this._overlapped != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(this._overlapped);
        }
    }
}
