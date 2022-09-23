using System.Threading;

namespace HashCalculator
{
    internal static class SerialGenerator
    {
        private static int serialNum = 0;

        public static void Reset()
        {
            serialNum = 0;
        }

        public static int GetSerial()
        {
            return ++serialNum;
        }

        public static void SerialBack()
        {
            --serialNum;
        }
    }

    internal static class CompletionCounter
    {
        private static int CompletedCount = 0;
        private static readonly object locker = new object();

        public static void Increment()
        {
            lock (locker)
            {
                ++CompletedCount;
            }
        }

        public static void Decrement()
        {
            lock (locker)
            {
                --CompletedCount;
            }
        }

        public static int Count()
        {
            lock (locker)
            {
                return CompletedCount;
            }
        }

        public static void ResetCount()
        {
            lock (locker)
            {
                CompletedCount = 0;
            }
        }
    }

    internal static class Locks
    {
        public static readonly object MainLock = new object();
        public static readonly object AlgoSelectionLock = new object();
        public static Semaphore ComputeLock = new Semaphore(4, 4);

        public static void UpdateComputeLock()
        {
            int semaphoreCount;
            switch (Settings.Current.SimulCalculate)
            {
                case SimCalc.One:
                    semaphoreCount = 1;
                    break;
                case SimCalc.Two:
                    semaphoreCount = 2;
                    break;
                case SimCalc.Four:
                    semaphoreCount = 4;
                    break;
                case SimCalc.Eight:
                    semaphoreCount = 8;
                    break;
                default:
                    semaphoreCount = 4;
                    break;
            }
            ComputeLock = new Semaphore(semaphoreCount, semaphoreCount);
        }
    }
}
