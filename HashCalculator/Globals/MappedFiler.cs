using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"创建互斥锁失败，程序将退出：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                Environment.Exit(1);
            }
            try
            {
                MappedFile = MemoryMappedFile.CreateOrOpen(
                    Info.MappedGuid, maxLength, MemoryMappedFileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"创建共享内存失败，程序将退出：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                Environment.Exit(2);
            }
        }

        public static bool ProcFlagCrossProcess
        {
            get => GetCrossProcessProcFlag();
            set => SetCrossProcessProcFlag(value);
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
            if (args != null && args.Length > 0)
            {
                if (!Settings.Current.RunInMultiInstanceMode)
                {
                    InternalPushArgs(args);
                    Synchronizer.Set();
                }
                else
                {
                    MainWindow.FlagComputeInProcessFiles = true;
                }
            }
            if (!newSync && !Settings.Current.RunInMultiInstanceMode)
            {
                if (MappedFiler.ProcFlagCrossProcess)
                {
                    Environment.Exit(0);
                }
            }
        }

        private static bool GetCrossProcessProcFlag()
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            {
                using (MappedReader reader = new MappedReader(mmvs))
                {
                    return reader.ReadProcFlag();
                }
            }
        }

        private static void SetCrossProcessProcFlag(bool exists)
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            {
                using (MappedWriter writer = new MappedWriter(mmvs))
                {
                    writer.WriteProcFlag(exists);
                }
            }
        }

        private static void InternalPushArgs(IEnumerable<string> args)
        {
            using (MemoryMappedViewStream mmvs = MappedFile.CreateViewStream())
            {
                using (MappedWriter writer = new MappedWriter(mmvs))
                {
                    writer.WriteLines(args.ToArray());
                }
            }
        }

        private const long maxLength = 4194304L;
        private static readonly bool newSync = false;

        public static MemoryMappedFile MappedFile { get; private set; }

        public static ProcSynchronizer Synchronizer { get; private set; }
    }
}
