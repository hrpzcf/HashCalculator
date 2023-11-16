using System;
using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    /// <summary>
    /// 检查两个 IEnumerable<byte> 之间每对相同下标的 byte 是否都相等
    /// </summary>
    internal class BytesComparer : IEqualityComparer<IEnumerable<byte>>
    {
        public static BytesComparer Default { get; } = new BytesComparer();

        public bool Equals(IEnumerable<byte> a, IEnumerable<byte> b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }
            else
            {
                return a.SequenceEqual(b);
            }
        }

        /// <summary>
        /// 经验证，两个 IEnumerable<byte>，元素数量和值都相同但顺序不同，合并得到的 HashCode 不同
        /// </summary>
        public int GetHashCode(IEnumerable<byte> bytes)
        {
            HashCode enumerableBytesHashCodeBuilder = new HashCode();
            if (bytes != null)
            {
                foreach (byte oneByteFromEnumerableBytes in bytes)
                {
                    enumerableBytesHashCodeBuilder.Add(oneByteFromEnumerableBytes);
                }
            }
            return enumerableBytesHashCodeBuilder.ToHashCode();
        }
    }
}
