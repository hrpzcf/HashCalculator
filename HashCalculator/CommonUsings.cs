using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Data;

namespace HashCalculator
{
    internal struct PathExpP
    {
        public PathExpP(string p)
        {
            this.filePath = p;
            this.expected = null;
        }

        public PathExpP(string[] hp)
        {
            this.filePath = hp[1];
            this.expected = hp[0];
        }

        public string filePath;
        public string expected;
    }

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

    internal class CmpResBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.NoResult:
                    return "#64888888";
                default:
                    return "Transparent";
            }
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
                return "待定";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class HashNameItems
    {
        private readonly Dictionary<string, string> hashNameItems =
            new Dictionary<string, string>();

        public void Clear()
        {
            this.hashNameItems.Clear();
        }

        public bool Add(string[] item)
        {
            if (item.Length < 2 || item[0] == null || item[1] == null)
                return false;
            string hash = item[0].Trim().ToUpper();
            // Windows 文件名不区分大小写
            string name = item[1].Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            this.hashNameItems[name] = hash;
            return true;
        }

        public CmpRes Compare(string hash, string name)
        {
            if (hash == null || name == null || this.hashNameItems.Count == 0)
                return CmpRes.NoResult;
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            hash = hash.Trim().ToUpper();
            if (this.hashNameItems.Keys.Count == 1 && this.hashNameItems.Keys.Contains(""))
                if (this.hashNameItems.Values.Contains(hash))
                    return CmpRes.Matched;
                else
                    return CmpRes.NoResult;
            if (this.hashNameItems.TryGetValue(name, out string dicHash))
                return dicHash == hash ? CmpRes.Matched : CmpRes.Mismatch;
            return CmpRes.NoResult;
        }
    }
}
