using System;
using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class HashBytesComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return x.SequenceEqual(y);
        }

        /// <summary>
        /// 经验证，两个 byte[]，byte 数量和值都相同但顺序不同，合并得到的 HashCode 是不同的，
        /// 没有在某些特殊情况下导致功能出错的风险
        /// </summary>
        public int GetHashCode(byte[] obj)
        {
            HashCode hashCodeFromBytes = new HashCode();
            foreach (byte hashByte in obj)
            {
                hashCodeFromBytes.Add(hashByte);
            }
            return hashCodeFromBytes.ToHashCode();
        }
    }
}
