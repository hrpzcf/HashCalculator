namespace HashCalculator
{
    /// <summary>
    /// 哈希计算任务模型的运行状态
    /// </summary>
    internal enum HashState
    {
        NoState,
        Waiting,
        Running,
        Paused,
        Finished,
    }

    /// <summary>
    /// 哈希计算任务模型的结果状态
    /// </summary>
    internal enum HashResult
    {
        NoResult,
        Canceled,
        Failed,
        Succeeded,
    }

    /// <summary>
    /// HashViewModel 的 PauseOrContinueModel 方法参数
    /// </summary>
    internal enum PauseMode
    {
        /// <summary>
        /// 暂停任务
        /// </summary>
        Pause,
        /// <summary>
        /// 继续任务
        /// </summary>
        Continue,
        /// <summary>
        /// 反转状态
        /// </summary>
        Invert,
    }

    /// <summary>
    /// 哈希值计算队列任务的完成状态，用于 MainWndViewModel 类
    /// </summary>
    internal enum QueueState
    {
        /// <summary>
        /// 启动前
        /// </summary>
        None,
        /// <summary>
        /// 队列开始
        /// </summary>
        Started,
        /// <summary>
        /// 队列结束
        /// </summary>
        Stopped,
    }

    internal enum CmpRes
    {
        /// <summary>
        /// 没有执行过比较操作
        /// </summary>
        NoResult,

        /// <summary>
        /// 执行过比较操作但没有关联项
        /// </summary>
        Unrelated,

        /// <summary>
        /// 执行过比较操作且已匹配
        /// </summary>
        Matched,

        /// <summary>
        /// 执行过比较操作但不匹配
        /// </summary>
        Mismatch,

        /// <summary>
        /// 执行过比较操作但未能确定是否匹配
        /// </summary>
        Uncertain,
    }

    /// <summary>
    /// 对文件夹的搜索策略
    /// </summary>
    public enum SearchPolicy
    {
        Children,
        Descendants,
        DontSearch,
    }

    /// <summary>
    /// 任务数量限制（同时计算多少个文件的哈希值）
    /// </summary>
    public enum TaskNum
    {
        One = 1,
        Two = 2,
        Four = 4,
        Eight = 8,
    }

    /// <summary>
    /// 哈希算法类型，Unknown 是创建 HashViewModel 实例时的默认值
    /// </summary>
    public enum AlgoType
    {
        SHA1,
        SHA224,
        SHA256,
        SHA384,
        SHA512,
        SHA3_224,
        SHA3_256,
        SHA3_384,
        SHA3_512,
        MD5,
        BLAKE2s,
        BLAKE2b,
        BLAKE3,
        Whirlpool,
        Unknown = -1,
    }

    public enum OutputType
    {
        BASE64,
        BinaryUpper,
        BinaryLower,
        Unknown = -1,
    }
}
