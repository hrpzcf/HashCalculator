using System;
using System.Collections.Generic;

namespace HashCalculator;

/// <summary>
/// 两个 byte[] 之间的内容相等性对比器
/// </summary>
internal class BytesComparer : IEqualityComparer<byte[]>
{
    public static BytesComparer Default { get; } = new BytesComparer();

    public bool Equals(byte[] a, byte[] b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }
        else
        {
            return a.SequenceEqual(b.AsSpan());
        }
    }

    /// <summary>
    /// 经验证，两个仅元素顺序不同的 byte[]，合并得到的 HashCode 不同
    /// </summary>
    public int GetHashCode(byte[] bytes)
    {
        HashCode hashCodeBuilder = new HashCode();
        hashCodeBuilder.AddBytes(bytes ?? Array.Empty<byte>());
        return hashCodeBuilder.ToHashCode();
    }
}
