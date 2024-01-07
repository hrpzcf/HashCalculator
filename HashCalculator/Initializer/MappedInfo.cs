namespace HashCalculator
{
    /// <summary>
    /// 各字段在 MemoryMappedFile 中占用的字节数
    /// </summary>
    internal static class MappedBytes
    {
        public const int Version = sizeof(int);
        public const int ProcessId = sizeof(int);
        public const int RunMulti = sizeof(bool);
        public const int ItemCount = sizeof(int);
        public const int ItemLength = sizeof(int);
        // Win32 API CreateProcessW 的 lpCommandLine 参数最多允许 32767 个字符
        // Version + ProcessId + RunMulti + ItemCount + ItemLength * 32767 + sizeof(char) * 32767 < 262144
        // ItemLength * 32767: 最坏的情况是每个字符串配一个指示字符串长度的 ItemLength
        public const long MaxLength = 262144;
    }

    /// <summary>
    /// 各字段在 MemoryMappedFile 中的起始位置，从 0 开始
    /// </summary>
    internal static class MappedStart
    {
        public static readonly int Version = 0;
        public static readonly int ProcessId = Version + MappedBytes.Version;
        public static readonly int RunMulti = ProcessId + MappedBytes.ProcessId;
        public static readonly int ItemCount = RunMulti + MappedBytes.RunMulti;
        public static readonly int FirstItem = ItemCount + MappedBytes.ItemCount;
    }
}
