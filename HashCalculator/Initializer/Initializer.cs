using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Windows;

namespace HashCalculator
{
    internal static class Initializer
    {
        static Initializer()
        {
            try
            {
                Synchronizer = new ProcSynchronizer(Info.AppGuid, false, out newSync);
                PIdSynchronizer = new ProcSynchronizer(Info.ProcIdGuid, false);
                MappedFile = MemoryMappedFile.CreateOrOpen(
                    Info.MappedGuid, MappedBytes.MaxLength, MemoryMappedFileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"内存映射文件初始化失败，即将退出：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                Environment.Exit(2);
            }
        }

        public static IEnumerable<string> GetArgs()
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            using (MmvsReader reader = new MmvsReader(mmvs))
            {
                foreach (string line in reader.ReadLines())
                {
                    yield return line;
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
                        CommonUtils.ShowWindowForeground(processId);
                    }
                    catch (Exception) { }
                }
                Environment.Exit(0);
            }
        }

        private static void InternalPushArgs(string[] args)
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            using (MmvsWriter writer = new MmvsWriter(mmvs))
            {
                writer.WriteLines(args);
            }
        }

        public static int ExistingProcessId
        {
            get
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                using (MmvsReader reader = new MmvsReader(mmvs))
                {
                    return reader.ReadProcessId();
                }
            }
            set
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                using (MmvsWriter writer = new MmvsWriter(mmvs))
                {
                    writer.WriteProcessId(value);
                }
            }
        }

        public static bool RunMultiMode
        {
            get
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                using (MmvsReader reader = new MmvsReader(mmvs))
                {
                    return reader.ReadRunMulti();
                }
            }
            set
            {
                using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
                using (MmvsWriter writer = new MmvsWriter(mmvs))
                {
                    writer.WriteRunMulti(value);
                }
            }
        }

        private static readonly bool newSync = false;

        public static MemoryMappedFile MappedFile { get; private set; }

        public static ProcSynchronizer Synchronizer { get; private set; }

        public static ProcSynchronizer PIdSynchronizer { get; private set; }
    }
}
