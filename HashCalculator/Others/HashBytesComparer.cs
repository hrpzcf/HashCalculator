using System;
using System.Collections.Generic;
using System.Linq;

namespace HashCalculator
{
    internal class HashBytesComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (y == null)
                {
                    return false;
                }
                else
                {
                    return x.Length == y.Length && x.SequenceEqual(y);
                }
            }
        }

        /// <summary>
        /// 经验证，两个 byte[]，byte 数量和值都相同但顺序不同，合并得到的 HashCode 是不同的，
        /// 没有在某些特殊情况下导致功能出错的风险
        /// </summary>
        public int GetHashCode(byte[] bytes)
        {
            HashCode hashCodeFromBytes = new HashCode();
            if (bytes != null)
            {
                foreach (byte byteFromHashValue in bytes)
                {
                    hashCodeFromBytes.Add(byteFromHashValue);
                }
            }
            return hashCodeFromBytes.ToHashCode();
        }
    }
}
