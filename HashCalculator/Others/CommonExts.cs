using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    }
}
