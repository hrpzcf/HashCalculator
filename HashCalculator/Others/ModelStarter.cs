using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator
{
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
            {
                return;
            }
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
            {
                return;
            }
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
//#if DEBUG
//            Task.Run(() =>
//            {
//                while (true)
//                {
//                    Console.WriteLine($"存活任务数：{this.Count}");
//                    Thread.Sleep(1000);
//                }
//            });
//#endif
        }

        public void Adjust(int number)
        {
            if (number < 1 || number > this.maxLength)
            {
                return;
            }
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
                    if (remaining <= 0)
                    {
                        break;
                    }
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
                    if (remaining <= 0)
                    {
                        break;
                    }
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
