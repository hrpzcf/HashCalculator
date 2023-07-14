using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Windows;

namespace HashCalculator
{
    internal static class MappedFiler
    {
        static MappedFiler()
        {
            try
            {
                Synchronizer =
                    new ProcSynchronizer(Info.AppGuid, false, out newSync);
                PIdSynchronizer = new ProcSynchronizer(Info.ProcIdGuid, false);
                if (newSync)
                {
                    MappedFile = MemoryMappedFile.CreateNew(
                        Info.MappedGuid, MappedBytes.MaxLength, MemoryMappedFileAccess.ReadWrite);
                }
                else
                {
                    MappedFile = MemoryMappedFile.OpenExisting(
                        Info.MappedGuid, MemoryMappedFileRights.ReadWrite);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"初始化失败，程序将退出：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                Environment.Exit(2);
            }
        }

        public static IEnumerable<string> GetArgs()
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            {
                using (MappedReader reader = new MappedReader(mmvs))
                {
                    foreach (string line in reader.ReadLines())
                    {
                        yield return line;
                    }
                }
            }
        }

        public static void PushArgs(string[] args)
        {
            if (newSync)
            {
                RunMultiMode = Settings.Current.RunInMultiInstMode;
                PIdSynchronizer.Set();
            }
            else
            {
                Settings.Current.RunInMultiInstMode = RunMultiMode;
            }
            if (args != null && args.Length > 0)
            {
                if (!Settings.Current.RunInMultiInstMode)
                {
                    InternalPushArgs(args);
                    Synchronizer.Set();
                }
                else
                {
                    MainWindow.PushStartupArgs(args);
                }
            }
            if (!newSync && !Settings.Current.RunInMultiInstMode)
            {
                int processId = ExistingProcessId;
                if (processId != default)
                {
                    try
                    {
                        Process process = Process.GetProcessById(processId);
                        IntPtr mainWndHandle = process.MainWindowHandle;
                        if (NativeFunctions.IsWindowVisible(mainWndHandle))
                        {
                            if (NativeFunctions.IsIconic(mainWndHandle))
                            {
                                NativeFunctions.ShowWindow(mainWndHandle, 9); // 9: SW_RESTORE
                            }
                            else
                            {
                                NativeFunctions.SetForegroundWindow(mainWndHandle);
                            }
                        }
                        else
                        {
                            NativeFunctions.ShowWindow(mainWndHandle, 5); // 5: SW_SHOW
                        }
                    }
                    catch (Exception) { }
                }
                Environment.Exit(0);
            }
        }

        private static void InternalPushArgs(string[] args)
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            {
                using (MappedWriter writer = new MappedWriter(mmvs))
                {
                    writer.WriteLines(args);
                }
            }
        }

        public static int ExistingProcessId
        {
            get
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                {
                    using (MappedReader reader = new MappedReader(mmvs))
                    {
                        return reader.ReadProcessId();
                    }
                }
            }
            set
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                {
                    using (MappedWriter writer = new MappedWriter(mmvs))
                    {
                        writer.WriteProcessId(value);
                    }
                }
            }
        }

        public static bool RunMultiMode
        {
            get
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                {
                    using (MappedReader reader = new MappedReader(mmvs))
                    {
                        return reader.ReadRunMulti();
                    }
                }
            }
            set
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                {
                    using (MappedWriter writer = new MappedWriter(mmvs))
                    {
                        writer.WriteRunMulti(value);
                    }
                }
            }
        }

        private static readonly bool newSync = false;

        public static MemoryMappedFile MappedFile { get; private set; }

        public static ProcSynchronizer Synchronizer { get; private set; }

        public static ProcSynchronizer PIdSynchronizer { get; private set; }
    }
}
