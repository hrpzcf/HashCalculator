using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator
{
    internal static class Locks
    {
        public static readonly object AlgoSelectionLock = new object();
        public static readonly object OutTypeSelectionLock = new object();
        public static readonly object ExportOptionsLock = new object();
    }

    /// <summary>
    /// 文件哈希值校验的依据，此依据从文件解析而来
    /// </summary>
    internal class Basis
    {
        private readonly Dictionary<string, List<string>> nameHashs =
            new Dictionary<string, List<string>>();

        public void Clear()
        {
            this.nameHashs.Clear();
        }

        public bool Add(string[] hashName)
        {
            if (hashName.Length < 2 || hashName[0] == null || hashName[1] == null)
                return false;
            string hash = hashName[0].Trim().ToLower();
            // Windows 文件名不区分大小写
            string name = hashName[1].Trim(new char[] { '*', ' ', '\n' }).ToLower();
            if (this.nameHashs.ContainsKey(name))
                this.nameHashs[name].Add(hash);
            else
                this.nameHashs[name] = new List<string> { hash };
            return true;
        }

        public CmpRes Verify(string name, string hash)
        {
            if (hash == null || name == null || this.nameHashs.Count == 0)
                return CmpRes.Unrelated;
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToLower();
            hash = hash.Trim().ToLower();
            if (this.nameHashs.Keys.Count == 1
                && this.nameHashs.Keys.Contains(string.Empty))
                if (this.nameHashs[string.Empty].Contains(hash))
                    return CmpRes.Matched;
                else
                    return CmpRes.Unrelated;
            if (!this.nameHashs.TryGetValue(name, out List<string> hashs))
                return CmpRes.Unrelated;
            if (hashs.Count == 0) return CmpRes.Uncertain;
            if (hashs.Count > 1)
            {
                string fst = hashs.First();
                if (hashs.All(i => i == fst))
                    return fst == hash ? CmpRes.Matched : CmpRes.Mismatch;
                else
                    return CmpRes.Uncertain;
            }
            return hashs.Contains(hash) ? CmpRes.Matched : CmpRes.Mismatch;
        }
    }

    internal static class GlobalCancellation
    {
        private static CancellationTokenSource cancellation;
        private static readonly object _lock = new object();

        public static void ResetHandler()
        {
            lock (_lock)
            {
                cancellation?.Dispose();
                cancellation = null;
            }
        }

        public static CancellationTokenSource Handle
        {
            get
            {
                lock (_lock)
                {
                    if (cancellation == null || cancellation.IsCancellationRequested)
                        cancellation = new CancellationTokenSource();
                    return cancellation;
                }
            }
        }
    }

    internal class UnitCvt
    {
        private const double kb = 1024D;
        private const double mb = 1048576D;
        private const double gb = 1073741824D;

        public static string FileSizeCvt(long bytes)
        {
            double bytesto;
            bytesto = bytes / gb;
            if (bytesto >= 1)
                return $"{bytesto:f1}GB";
            bytesto = bytes / mb;
            if (bytesto >= 1)
                return $"{bytesto:f1}MB";
            bytesto = bytes / kb;
            if (bytesto >= 1)
                return $"{bytesto:f1}KB";
            return $"{bytes}B";
        }
    }

    internal class TaskKeeper
    {
        private CancellationTokenSource source;
        private Task task;
        private readonly Action<CancellationToken> process;

        public TaskKeeper(Action<CancellationToken> process)
        {
            this.process = process;
        }

        public void Reset()
        {
            if (this.IsAlive())
                return;
            this.source = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(
                () => { this.process(this.source.Token); },
                this.source.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Cancel()
        {
            if (this.source == null || this.task == null)
                return;
            this.source.Cancel();
            this.task.Wait();
            this.source.Dispose();
            this.task = null;
            this.source = null;
        }

        public bool IsAlive()
        {
            return this.source != null && this.task != null;
        }
    }

    internal class ModelStarter
    {
        private readonly int maxLength;
        private readonly BlockingCollection<HashViewModel> queue
            = new BlockingCollection<HashViewModel>();
        private readonly TaskKeeper[] keepers;

        public ModelStarter(int maxLength)
        {
            this.maxLength = maxLength < 1 ? 1 : maxLength;
            this.keepers = new TaskKeeper[maxLength];
            for (int i = 0; i < maxLength; ++i)
            {
                this.keepers[i] = new TaskKeeper(this.ModelWatcher);
            }
#if DEBUG
            Task.Run(this.ShowAliveTasksCount);
#endif
        }

#if DEBUG
        private void ShowAliveTasksCount()
        {
            while (true)
            {
                Console.WriteLine($"存活任务数：{this.Count}");
                Thread.Sleep(1000);
            }
        }
#endif

        public void Adjust(int number)
        {
            if (number < 1 || number > this.maxLength)
                return;
            int count = this.Count;
            if (count < number)
            {
                int remaining = number - count;
                foreach (TaskKeeper tk in this.keepers)
                {
                    if (!tk.IsAlive())
                    {
                        tk.Reset();
                        --remaining;
                    }
                    if (remaining <= 0) break;
                }
            }
            else if (count > number)
            {
                int remaining = count - number;
                foreach (TaskKeeper tk in this.keepers)
                {
                    if (tk.IsAlive())
                    {
                        tk.Cancel();
                        --remaining;
                    }
                    if (remaining <= 0) break;
                }
            }
        }

        private void ModelWatcher(CancellationToken ct)
        {
            HashViewModel m;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    m = this.queue.Take(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                m.ComputeManyHashValue();
            }
        }

        public void PendingModel(HashViewModel model)
        {
            if (!this.queue.Contains(model))
            {
                this.queue.Add(model);
            }
#if DEBUG
            else
            {
                Console.WriteLine("当前队列已包含相同的元素");
            }
#endif
        }

        public int Count
        {
            get { return this.keepers.Count(i => i.IsAlive()); }
        }
    }
}
