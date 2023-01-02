using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace HashCalculator
{
    internal struct ModelArg
    {
        public ModelArg(string[] hashpath)
        {
            this.useless = false;
            this.tokenSrc = null;
            this.filePath = hashpath[1];
            this.expected = hashpath[0];
        }

        /// <summary>
        /// 约定：h 为空字符串则表示无法确定是否匹配
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        public ModelArg(string hash, string path)
        {
            this.useless = false;
            this.tokenSrc = null;
            this.filePath = path;
            this.expected = hash;
        }

        public ModelArg(string path)
        {
            this.useless = false;
            this.tokenSrc = null;
            this.filePath = path;
            this.expected = null;
        }

        public ModelArg(string path, bool useless)
        {
            this.useless = useless;
            this.tokenSrc = null;
            this.filePath = path;
            this.expected = null;
        }

        public string filePath;
        public bool useless;
        public string expected;
        public CancellationTokenSource tokenSrc;
    }

    internal static class SerialGenerator
    {
        private static int serialNum = 0;
        private static readonly object locker = new object();

        public static void Reset()
        {
            lock (locker) { serialNum = 0; }
        }

        public static int GetSerial()
        {
            lock (locker) { return ++serialNum; }
        }

        public static void SerialBack()
        {
            lock (locker) { --serialNum; }
        }
    }

    internal static class Locks
    {
        public static readonly object AlgoSelectionLock = new object();
        public static readonly AutoResetEvent CancelTasksAutoResetLock
            = new AutoResetEvent(false);
        public static readonly object EnqueueDequeueLock = new object();
    }

    internal class CmpResFgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            if (!Settings.Current.ShowResultText)
                return "Transparent";
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return "Black";
                case CmpRes.Matched:
                    return "White";
                case CmpRes.Mismatch:
                    return "White";
                case CmpRes.Uncertain:
                    return "White";
                case CmpRes.NoResult:
                default:
                    return "Transparent";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.Unrelated; // 此处未使用，只返回默认值
        }
    }

    internal class CmpResBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return "#64888888";
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.Uncertain:
                    return "Black";
                case CmpRes.NoResult:
                default:
                    return "Transparent";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.Unrelated; // 此处未使用，只返回默认值
        }
    }

    internal class CmpResTextCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return "无关联";
                case CmpRes.Matched:
                    return "已匹配";
                case CmpRes.Mismatch:
                    return "不匹配";
                case CmpRes.Uncertain:
                    return "不确定";
                case CmpRes.NoResult:
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class CmpResBorderCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            if (Settings.Current.ShowResultText)
                return "0";
            else
                return "3";
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class AlgoTypeBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((AlgoType)value)
            {
                case AlgoType.SHA256:
                    return "#640066FF";
                case AlgoType.SHA1:
                    return "#64FF0071";
                case AlgoType.SHA224:
                    return "#64331772";
                case AlgoType.SHA384:
                    return "#64FFBB33";
                case AlgoType.SHA512:
                    return "#64008B73";
                case AlgoType.MD5:
                    return "#64799B00";
                default:
                    return "#64FF0000";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AlgoType.SHA256; // 此处未使用，只返回默认值
        }
    }

    internal class AlgoTypeNameCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((AlgoType)value == AlgoType.Unknown)
                return "算法未定";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
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

    public class ToolTipReport : DependencyObject
    {
        public static ToolTipReport Instance;
        private static readonly DependencyProperty ReportProperty
            = DependencyProperty.Register(
                "Report",
                typeof(string),
                typeof(ToolTipReport),
                new PropertyMetadata("暂无校验报告")
        );

        public ToolTipReport() { Instance = this; }

        public string Report
        {
            set { this.SetValue(ReportProperty, value); }
            get { return (string)this.GetValue(ReportProperty); }
        }
    }

    internal static class ModelTaskHelper
    {
        #region 计数器相关字段
        public static event Action<int> StartingEvent;
        public static event Action<int> IncreaseEvent;
        public static event Action<int> FinishedEvent;
        public static event Action<int> MaxCountEvent;

        private static readonly object counterLock = new object();
        private static bool isInactive = true;
        private static int maxCount = 0;
        private static int completedCount = 0;
        private static int queueRequestCount = 0;
        #endregion

        private static readonly object runTaskLock = new object();
        private static int runningTaskCount = 0;
        private static int maxRunningCount = 2;
        private static readonly BlockingCollection<Task> CalcTasks
            = new BlockingCollection<Task>();
        private static readonly BlockingCollection<ModelArg> ModelArgs
            = new BlockingCollection<ModelArg>();
        private static readonly Action<int, ModelArg> AHM
             = new Action<int, ModelArg>(AddModleMethod);
        private static Action<HashModel> AddModelCallback;
        private static readonly object cancellationLock = new object();
        private static CancellationTokenSource cancellation
            = new CancellationTokenSource();

        /// <summary>
        /// 读取设置并更新同时运行的计算任务数
        /// </summary>
        public static void RefreshTaskLimit()
        {
            lock (runTaskLock)
            {
                switch (Settings.Current.TaskLimit)
                {
                    case SimCalc.One:
                        maxRunningCount = 1;
                        break;
                    case SimCalc.Two:
                        maxRunningCount = 2;
                        break;
                    default:
                    case SimCalc.Four:
                        maxRunningCount = 4;
                        break;
                    case SimCalc.Eight:
                        maxRunningCount = 8;
                        break;
                }
            }
        }

        public static void CounterMax(int num)
        {
            lock (counterLock)
            {
                if (isInactive)
                {
                    isInactive = false;
                    completedCount = 0;
                    maxCount = num > 0 ? num : 0;
                }
                else
                {
                    maxCount += num;
                    if (num < 0 && maxCount <= completedCount)
                        FinishedEvent(completedCount);
                }
                MaxCountEvent(maxCount);
            }
        }

        public static void CounterReset()
        {
            lock (counterLock)
            {
                isInactive = true;
                completedCount = 0;
                maxCount = 0;
                FinishedEvent(completedCount);
            }
        }

        public static void CounterIncrease()
        {
            lock (counterLock)
            {
                if (isInactive || maxCount <= 0 || completedCount > maxCount)
                    return;
                if (completedCount == 0)
                    StartingEvent(completedCount);
                if (++completedCount < maxCount)
                    IncreaseEvent(completedCount);
                else
                {
                    isInactive = true;
                    FinishedEvent(completedCount);
                }
            }
        }

        public static void CounterDecrease()
        {
            lock (counterLock)
            {
                if (--completedCount < maxCount) isInactive = false;
            }
        }

        public static void ReleaseTask()
        {
            lock (runTaskLock) --runningTaskCount;
        }

        public static void SendRequestToQueueArgs()
        {
            lock (cancellationLock)
            {
                if (queueRequestCount >= 0)
                    ++queueRequestCount;
                else
                    queueRequestCount = 1;
            }
        }

        public static void QueueModelArgs(IEnumerable<ModelArg> args)
        {
            lock (cancellationLock)
            {
                if (queueRequestCount <= 0)
                    return;
                foreach (ModelArg ma in args) ModelArgs.Add(ma);
                --queueRequestCount;
            }
        }

        private static void AddModleMethod(int serial, ModelArg modelArg)
        {
            lock (cancellationLock)
            {
                // 原 tokenSrc 使用关联了 cancellation.Token 的新 source 是因为：
                // AddModelMonitor 的 Take 后、AddModleMethod 之前执行完取消操作
                // 仍然可以保证这最后一个任务可以被 cancellation.Token 取消，要不然就漏网
                CancellationTokenSource ctsLinked =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        modelArg.tokenSrc.Token, cancellation.Token);
                modelArg.tokenSrc = ctsLinked;
                HashModel hm = new HashModel(serial, modelArg);
                ctsLinked.Token.Register(hm.Cancelled);
                CalcTasks.Add(new Task(hm.EnterGenerateUnderLimit, ctsLinked.Token));
                AddModelCallback(hm);
            }
        }

        public static void AddModelMonitor()
        {
            while (true)
            {
                // XXX: 如果执行了 Take 后发生取消任务事件
                ModelArg arg = ModelArgs.Take();
                int serial = SerialGenerator.GetSerial();
                Application.Current.Dispatcher.Invoke(AHM, serial, arg);
                Thread.Sleep(10);
            }
        }

        private static void TaskStartMonitor()
        {
            while (true)
            {
                lock (runTaskLock)
                {
                    if (runningTaskCount < maxRunningCount
                        && CalcTasks.TryTake(out Task t, 1000)
                        && !t.IsCompleted)
                    {
                        ++runningTaskCount; t.Start();
                    }
                }
                Thread.Sleep(10);
            }
        }

        public static void CancelTasks()
        {
            lock (cancellationLock)
            {
                queueRequestCount = 0;
                while (ModelArgs.Count > 0)
                    ModelArgs.TryTake(out _);
                cancellation.Cancel();
                while (CalcTasks.Count > 0)
                    CalcTasks.TryTake(out _);
                CounterReset();
                MessageBox.Show("正在执行的任务和排队中的任务已全部取消。");
                cancellation = new CancellationTokenSource();
            }
        }

        public static void InitializeHelper(Action<HashModel> action)
        {
            AddModelCallback = action;
            new Thread(AddModelMonitor) { IsBackground = true }.Start();
            new Thread(TaskStartMonitor) { IsBackground = true }.Start();
        }
    }
}
