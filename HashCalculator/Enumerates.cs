using System;

namespace HashCalculator
{
    /// <summary>
    /// 哈希计算任务模型的运行状态
    /// </summary>
    internal enum HashState
    {
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
        Succeeded,
        Canceled,
        HasFailed,
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
    /// 哈希值计算队列任务的完成状态，用于 AppViewModel 类
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
    [Serializable]
    internal enum SearchPolicy
    {
        Children,
        Descendants,
        DontSearch,
    }

    /// <summary>
    /// 同时计算多少个文件的哈希值
    /// </summary>
    [Serializable]
    internal enum SimCalc
    {
        One,
        Two,
        Four,
        Eight,
    }

    /// <summary>
    /// 哈希算法类型，Unknown 是创建 HashModule 实例时的默认值
    /// </summary>
    [Serializable]
    internal enum AlgoType
    {
        SHA256,
        SHA1,
        SHA224,
        SHA384,
        SHA512,
        MD5,
        Unknown = -1,
    }
}
