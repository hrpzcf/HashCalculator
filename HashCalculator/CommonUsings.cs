using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Data;

namespace HashCalculator
{
    internal struct PathExp
    {
        public PathExp(string[] hp)
        {
            this.useless = false;
            this.filePath = hp[1];
            this.expected = hp[0];
        }

        public PathExp(string p)
        {
            this.useless = false;
            this.filePath = p;
            this.expected = null;
        }

        public PathExp(string p, bool useless)
        {
            this.useless = useless;
            this.filePath = p;
            this.expected = null;
        }

        public string filePath;
        public bool useless;
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
                case CmpRes.Unrelated:
                    return "#64888888";
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.Uncertain:
                    return "black";
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
                return "未知";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // BUG 当一个哈希值文件中有同名文件时，此类比较方法就可能会出错
    internal class NameHashItems
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
            string hash = hashName[0].Trim().ToUpper();
            // Windows 文件名不区分大小写
            string name = hashName[1].Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            if (nameHashs.ContainsKey(name))
                this.nameHashs[name].Add(hash);
            else
                this.nameHashs[name] = new List<string> { hash };
            return true;
        }

        public CmpRes Compare(string name, string hash)
        {
            if (hash == null || name == null || this.nameHashs.Count == 0)
                return CmpRes.Unrelated;
            // Windows 文件名不区分大小写
            name = name.Trim(new char[] { '*', ' ', '\n' }).ToUpper();
            hash = hash.Trim().ToUpper();
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
}
