using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HashCalculator
{
    internal delegate bool RefMethod(ref CancellationToken token);

    internal class HashTask
    {
        public HashTask(Action<CancellationToken, RefMethod> process)
        {
            this.process = process;
        }

        private bool Refresh(ref CancellationToken token)
        {
            this.tokenSource = new CancellationTokenSource();
            token = this.tokenSource.Token;
            return true;
        }

        public void Startup()
        {
            this.tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
                {
                    this.process(
                        this.tokenSource.Token, this.Refresh);
                },
                this.tokenSource.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Shutdown()
        {
            this.tokenSource?.Cancel();
        }

        public bool IsAlive()
        {
            return this.tokenSource != null && !this.tokenSource.IsCancellationRequested;
        }

        private CancellationTokenSource tokenSource;
        private readonly Action<CancellationToken, RefMethod> process;
    }

    internal class ModelStarter
    {
        public ModelStarter(int initCount, int maxCount)
        {
            this.maxCount = maxCount < 1 ? 1 : maxCount;
            this.hashTasks = new HashTask[maxCount];
            for (int i = 0; i < maxCount; ++i)
            {
                this.hashTasks[i] = new HashTask(this.HashProcess);
            }
            this.BeginAdjust(initCount);
        }

        public async void BeginAdjust(int count)
        {
            await Task.Run(() =>
            {
                if (count < 1 || count > this.maxCount)
                {
                    return;
                }
                Monitor.Enter(this.changeCountLock);
                this.targetCount = count;
                if (this.taskCount < this.targetCount)
                {
                    int remaining = this.targetCount - this.taskCount;
                    foreach (HashTask mt in this.hashTasks)
                    {
                        if (mt.IsAlive())
                        {
                            continue;
                        }
                        mt.Startup();
                        if (--remaining <= 0)
                        {
                            break;
                        }
                    }
                }
                else if (this.taskCount > this.targetCount)
                {
                    foreach (HashTask mt in this.hashTasks)
                    {
                        mt.Shutdown();
                    }
                }
                Monitor.Exit(this.changeCountLock);
            });
        }

        public void PendingModel(HashViewModel model)
        {
            model.State = HashState.Waiting;
            if (this.queue.Contains(model))
            {
#if DEBUG
                Console.WriteLine("队列中已包含相同的元素...");
#endif
                return;
            }
            this.queue.Add(model);
        }

        private void HashProcess(CancellationToken token, RefMethod refreshCancellationToken)
        {
            ++this.taskCount;
            while (true)
            {
                Monitor.Enter(this.changeCountLock);
                if (this.targetCount < this.taskCount)
                {
                    --this.taskCount;
                    Monitor.Exit(this.changeCountLock);
                    break;
                }
                else if (token.IsCancellationRequested)
                {
                    refreshCancellationToken(ref token);
                }
                Monitor.Exit(this.changeCountLock);
                try
                {
                    HashViewModel mmodel = this.queue.Take(token);
                    mmodel.ComputeManyHashValue();
                }
                catch (OperationCanceledException) { }
            }
        }

        private readonly int maxCount;
        private int taskCount = 0;
        private int targetCount = 0;
        private readonly object changeCountLock = new object();
        private readonly HashTask[] hashTasks;
        private readonly BlockingCollection<HashViewModel> queue = new BlockingCollection<HashViewModel>();
    }
}
