using System;
using System.Collections.Generic;

namespace HashCalculator;

/// <summary>
/// 文件图标缓存的键。<see cref="ImageListIndex"/> 为 -1 时表示"仅按扩展名"的快路径键
/// （此时尚未询问 shell）；为非负值时表示 shell 返回的系统图像列表索引，
/// 此时图标身份由该索引决定。
/// </summary>
internal readonly struct FileIconCacheKey : IEquatable<FileIconCacheKey>
{
    public readonly string Extension;
    public readonly int ImageListIndex;
    public readonly bool Large;

    public FileIconCacheKey(string extension, int imageListIndex, bool large)
    {
        this.Extension = extension;
        this.ImageListIndex = imageListIndex;
        this.Large = large;
    }

    /// <summary>
    /// 图标取自文件自身、不能按扩展名复用的类型：可执行文件、图标文件、快捷方式等。
    /// 名单漏项只会导致该类型的图标都显示成同类型中第一个文件的图标（视觉问题），
    /// 不影响程序正确性。空扩展名的文件也归入此类，其图标可能与文件本身有关。
    /// </summary>
    public static bool IsIconFromFileItself(string extension)
    {
        return iconFromFileItselfExtensions.Contains(extension);
    }

    private static readonly HashSet<string> iconFromFileItselfExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "",
            ".exe", ".com", ".scr", ".pif", ".dll", ".ocx", ".cpl", ".sys",
            ".msi", ".msp", ".lnk", ".ico", ".cur", ".ani", ".url",
        };

    // 判等与哈希都按"扩展名不区分大小写"处理：Windows 的文件类型关联本就不区分大小写。
    // 两者的策略必须保持一致，否则会出现"判等相等但哈希不同"，导致缓存查不到。
    public bool Equals(FileIconCacheKey other)
    {
        return this.ImageListIndex == other.ImageListIndex &&
            this.Large == other.Large &&
            string.Equals(this.Extension, other.Extension, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return obj is FileIconCacheKey other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.Extension == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(this.Extension),
            this.ImageListIndex,
            this.Large);
    }
}
