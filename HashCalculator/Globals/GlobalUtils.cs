using System;
using System.Buffers;

namespace HashCalculator
{
    internal static class GlobalUtils
    {
        private const int pageSize = 4096;
        private const int maxMultiple = 1024;

        /// <summary>
        /// 请确保 array 是 null 或通过 ArrayPool<T>.Shared.Rent 分配
        /// </summary>
        public static void MakeSureBuffer<T>(ref T[] array, int length)
        {
            if (array != null && length <= 0)
            {
                ArrayPool<T>.Shared.Return(array);
                array = null;
            }
            else if (array == null && length > 0)
            {
                array = ArrayPool<T>.Shared.Rent(length);
            }
            else if (array != null && length > array.Length)
            {
                ArrayPool<T>.Shared.Return(array);
                array = ArrayPool<T>.Shared.Rent(length);
            }
        }

        /// <summary>
        /// 根据文件大小给出建议大小的文件读取缓冲区，使 4MB 内小文件可以被一次读取
        /// </summary>
        public static void Suggest<T>(ref T[] array, long fileSize)
        {
            int multiple = Math.Min(maxMultiple, (int)(fileSize / pageSize + 1));
            MakeSureBuffer(ref array, pageSize * multiple);
        }
    }
}
