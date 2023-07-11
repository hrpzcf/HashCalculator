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
