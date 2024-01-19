using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HashCalculator
{
    internal static class CommonExts
    {
        /// <summary>
        /// 为 IList 类型对象提供低成本检查集合是否为空的扩展方法
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool AnyItem(this IList source)
        {
            if (source == null)
            {
                throw new ArgumentNullException($"Argument {nameof(source)} can not be null");
            }
            return source.GetEnumerator().MoveNext();
        }

        /// <summary>
        /// 为 FileInfo 对象提供检查父目录路径与指定字符串是否相同的扩展方法
        /// </summary>
        /// <param name="info"></param>
        /// <param name="toCompare"></param>
        /// <returns></returns>
        public static bool ParentSameWith(this FileInfo info, string toCompare)
        {
            return info.DirectoryName.Equals(toCompare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 为 string 对象提供扩展方法，该方法以该对象自身为连接符，将若干非 null 非空的字符串连接起来
        /// </summary>
        /// <param name="seperator"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string Join(this string seperator, params string[] values)
        {
            int indexOftheLast = values.Length - 1;
            var cleanlyJoinValuesStringBuilder = new StringBuilder();
            for (int i = 0; i < values.Length; ++i)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    cleanlyJoinValuesStringBuilder.Append(values[i]);
                    if (i != indexOftheLast)
                    {
                        cleanlyJoinValuesStringBuilder.Append(seperator);
                    }
                }
            }
            return cleanlyJoinValuesStringBuilder.ToString();
        }

        /// <summary>
        /// 为 HashViewModel 的 ModelCapturedEvent 和 ModelReleasedEvent 事件提供异步执行的扩展方法 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="model"></param>
        public static async void InvokeAsync(this Action<HashViewModel> action, HashViewModel model)
        {
            await Task.Run(() => { action(model); });
        }

        /// <summary>
        /// 为 IEnumerable<T> 类型提供返回 HashSet<T> 的 ToHashSet 扩展方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerale"></param>
        /// <returns></returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerale)
        {
            return new HashSet<T>(enumerale);
        }

        /// <summary>
        /// 为 IEnumerable<T> 类型提供返回 HashSet<T> 的 ToHashSet 扩展方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerale"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerale, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(enumerale, comparer);
        }

        /// <summary>
        /// 为 string 类型的文件完整路径提供获取文件唯一标识符的扩展方法
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static CmpableFileIndex GetFileIndex(this string filePath)
        {
            int INVALID_HANDLE_VALUE = -1;
            IntPtr fileHandle = new IntPtr(INVALID_HANDLE_VALUE);
            CmpableFileIndex fileIndex = default;
            if (!Path.IsPathRooted(filePath) || !File.Exists(filePath))
            {
                goto FinalizeAndReturnResult;
            }
            fileHandle = KERNEL32.CreateFileW(filePath, 0U,
                FileShare.Read | FileShare.Write | FileShare.Delete, IntPtr.Zero, FileMode.Open,
                FileAttributes.Normal | FileAttributes.ReparsePoint, IntPtr.Zero);
            if (fileHandle.ToInt32() == INVALID_HANDLE_VALUE)
            {
                goto FinalizeAndReturnResult;
            }
            if (!KERNEL32.GetFileInformationByHandle(fileHandle, out BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                goto FinalizeAndReturnResult;
            }
            fileIndex = new CmpableFileIndex(fileInfo);
        FinalizeAndReturnResult:
            if (fileHandle.ToInt32() != INVALID_HANDLE_VALUE)
            {
                KERNEL32.CloseHandle(fileHandle);
            }
            return fileIndex;
        }

        /// <summary>
        /// 为 IEnumerable<T> 对象提供与另一个 IEnumerable<T> 合并遍历子元素的扩展方法
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="enum1"></param>
        /// <param name="enum2"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<T1, T2>> ZipElements<T1, T2>(this IEnumerable<T1> enum1, IEnumerable<T2> enum2)
        {
            if (enum1 == null || enum2 == null)
            {
                yield break;
            }
            for (int index = 0; index < Math.Min(enum1.Count(), enum2.Count()); ++index)
            {
                yield return new Tuple<T1, T2>(enum1.ElementAt(index), enum2.ElementAt(index));
            }
        }

        public static void Extend(this SortDescriptionCollection collection, IEnumerable<SortDescription> descriptions)
        {
            if (descriptions != null)
            {
                foreach (SortDescription description in descriptions)
                {
                    collection.Add(description);
                }
            }
        }

        public static void Extend(this ObservableCollection<GroupDescription> collection, IEnumerable<GroupDescription> descriptions)
        {
            if (descriptions != null)
            {
                foreach (GroupDescription description in descriptions)
                {
                    collection.Add(description);
                }
            }
        }

        public static bool DeleteNode(this RegistryKey root, RegNode regNode)
        {
            if (root != null)
            {
                try
                {
                    if (regNode.Name != string.Empty)
                    {
                        root.DeleteSubKeyTree(regNode.Name, false);
                        return true;
                    }
                }
                catch (Exception) { }
            }
            return false;
        }

        public static bool WriteNode(this RegistryKey root, RegNode regNode)
        {
            if (root != null)
            {
                try
                {
                    using (RegistryKey parent = root.CreateSubKey(regNode.Name, true))
                    {
                        if (parent != null)
                        {
                            if (regNode.Nodes != null)
                            {
                                foreach (RegNode nextNode in regNode.Nodes)
                                {
                                    if (!parent.WriteNode(nextNode))
                                    {
                                        return false;
                                    }
                                }
                            }
                            if (regNode.Values != null)
                            {
                                foreach (RegValue nextValue in regNode.Values)
                                {
                                    parent.SetValue(nextValue.Name, nextValue.Data, nextValue.Kind);
                                }
                            }
                        }
                    }
                    return true;
                }
                catch (Exception) { }
            }
            return false;
        }

        /// <summary>
        /// 把 array 内所有的 oldValue 替换为 newValue。
        /// </summary>
        public static void Replace<T>(this T[] array, T oldValue, T newValue)
        {
            array.Replace(oldValue, newValue, 0, array.Length);
        }

        /// <summary>
        /// 在 array 数组中起始点为 offset，元素数量为 count 的范围内，把所有的 oldValue 替换为 newValue。
        /// </summary>
        public static void Replace<T>(this T[] array, T oldValue, T newValue, int offset, int count)
        {
            if (offset >= 0 && offset < array.Length && (offset + count) <= array.Length)
            {
                for (int index = offset; index < offset + count; ++index)
                {
                    if (array[index].Equals(oldValue))
                    {
                        array[index] = newValue;
                    }
                }
            }
        }

        /// <summary>
        /// Seq1.ElementsEqual(Seq2)，如果 Seq1 元素全部与 Seq2 相对应元素相等则为 true。<br/>
        /// 如果 Seq2 是 null 或 Seq1 比 Seq2 长，则结果肯定是 false；<br/>
        /// 如果 Seq1 比 Seq2 短，只要是 Seq1 元素全部与 Seq2 相对应元素相等，也视为 true。<br/>
        /// </summary>
        public static bool ElementsEqual<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            return first.ElementsEqual(second, null);
        }

        /// <summary>
        /// Seq1.ElementsEqual(Seq2)，如果 Seq1 所有元素与 Seq2 相应元素相等则为 true。<br/>
        /// 如果 Seq2 是 null 或 Seq1 比 Seq2 长，则结果肯定是 false；<br/>
        /// 如果 Seq1 比 Seq2 短，只要 Seq1 元素全部与 Seq2 相对应元素相等，也视为 true。<br/>
        /// </summary>
        public static bool ElementsEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }
            if (second == null)
            {
                return false;
            }
            using (IEnumerator<T> enumerator1 = first.GetEnumerator())
            using (IEnumerator<T> enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    if (!enumerator2.MoveNext() || !comparer.Equals(enumerator1.Current, enumerator2.Current))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
