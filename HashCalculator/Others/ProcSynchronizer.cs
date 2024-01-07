using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal class ProcSynchronizer
    {
        private const int INFINITE = unchecked((int)0xFFFFFFFF);
        private const int ERROR_ALREADY_EXISTS = 0x000000b7;
        private const int EVENT_ALL_ACCESS = 0x000F0000 | 0x00100000 | 0x3;
        private const int ERROR_INVALID_HANDLE = 0x00000006;
        private const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        private readonly string _eventName;
        private readonly IntPtr _eventHandle;

        public string Name => this._eventName;

        /// <summary>
        /// 行为类似 AutoResetEvent，但可以跨进程使用
        /// </summary>
        /// <param name="name">本内核对象的名称，想要跨进程使用则必须指定相同的名称</param>
        /// <param name="state">内本核对象的初始状态，true 是有信号，false 是无信号，有信号则 Wait 方法释放线程</param>
        public ProcSynchronizer(string name, bool state)
        {
            this._eventName = name;
            this._eventHandle = CreateEventW(IntPtr.Zero, false, state, name);
            if (IntPtr.Zero == this._eventHandle)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        /// <summary>
        /// 行为类似 AutoResetEvent，但可以跨进程使用
        /// </summary>
        /// <param name="name">本内核对象的名称，想要跨进程使用则必须指定相同的名称</param>
        /// <param name="state">内本核对象的初始状态，true 是有信号，false 是无信号，有信号则 Wait 方法释放线程</param>
        /// <param name="createdNew">类似 Mutex，指示是否创建了新的内核对象</param>
        public ProcSynchronizer(string name, bool state, out bool createdNew)
        {
            this._eventName = name;
            this._eventHandle = CreateEventW(IntPtr.Zero, false, state, name);
            if (IntPtr.Zero == this._eventHandle)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            createdNew = Marshal.GetLastWin32Error() != ERROR_ALREADY_EXISTS;
        }

        ~ProcSynchronizer()
        {
            KERNEL32.CloseHandle(this._eventHandle);
        }

        public bool Wait(int milliseconds = INFINITE)
        {
            return WaitForSingleObject(this._eventHandle, milliseconds) != WAIT_FAILED;
        }

        public bool Set()
        {
            return SetEvent(this._eventHandle);
        }

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/synchapi/nf-synchapi-createeventw
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateEventW(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/synchapi/nf-synchapi-setevent
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetEvent(IntPtr hEvent);

        /// <summary>
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);
    }
}
