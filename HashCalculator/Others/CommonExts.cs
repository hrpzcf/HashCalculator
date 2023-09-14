using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        /// 为 FileInfo 对象提供检查父目录路径与指定字符串是否相同的方法
        /// </summary>
        /// <param name="info"></param>
        /// <param name="toCompare"></param>
        /// <returns></returns>
        public static bool ParentSameWith(this FileInfo info, string toCompare)
        {
            return info.DirectoryName.Equals(toCompare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 为 HashViewModel 的 ModelCapturedEvent 和 ModelReleasedEvent 事件提供异步执行方法 
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
            fileHandle = NativeFunctions.CreateFileW(filePath, 0U, FileShare.Read | FileShare.Write | FileShare.Delete,
               IntPtr.Zero, FileMode.Open, FileAttributes.Normal | FileAttributes.ReparsePoint, IntPtr.Zero);
            if (fileHandle.ToInt32() == INVALID_HANDLE_VALUE)
            {
                goto FinalizeAndReturnResult;
            }
            if (!NativeFunctions.GetFileInformationByHandle(fileHandle, out BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                goto FinalizeAndReturnResult;
            }
            fileIndex = new CmpableFileIndex(fileInfo);
        FinalizeAndReturnResult:
            if (fileHandle.ToInt32() != INVALID_HANDLE_VALUE)
            {
                NativeFunctions.CloseHandle(fileHandle);
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
    }
}
