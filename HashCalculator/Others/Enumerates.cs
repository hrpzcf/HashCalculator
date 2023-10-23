namespace HashCalculator
{
    /// <summary>
    /// 哈希计算任务模型的运行状态
    /// </summary>
    internal enum HashState
    {
        /// <summary>
        /// HashViewModel 初始状态
        /// </summary>
        NoState,

        /// <summary>
        /// HashViewModel 已被加入待计算队列
        /// </summary>
        Waiting,

        /// <summary>
        /// HashViewModel 正在进行哈希值计算
        /// </summary>
        Running,

        /// <summary>
        /// HashViewModel 正进行的哈希计算已暂停
        /// </summary>
        Paused,

        /// <summary>
        /// HashViewModel 已结束哈希值的计算
        /// </summary>
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
    /// 同时进行的任务数量上限
    /// </summary>
    public enum TaskNum
    {
        One = 1,
        Two = 2,
        Four = 4,
        Eight = 8,
    }

    /// <summary>
    /// 哈希算法类型
    /// </summary>
    public enum AlgoType
    {
        // SHA1
        SHA1,
        // SHA2
        SHA224,
        SHA256,
        SHA384,
        SHA512,
        // SHA3
        SHA3, // 代表系列，下同
        SHA3_224,
        SHA3_256,
        SHA3_384,
        SHA3_512,
        // Streebog
        STREEBOG,
        STREEBOG_256,
        STREEBOG_512,
        // WHIRLPOOL
        WHIRLPOOL,
        // MD5
        MD5,
        // Crc32
        CRC32,
        // XxHash
        XXHASH32,
        XXHASH64,
        XXHASH3,
        XXHASH128,
        // BLAKE2S
        BLAKE2S,
        BLAKE2S_128,
        BLAKE2S_160,
        BLAKE2S_224,
        BLAKE2S_256,
        // BLAKE2SP
        BLAKE2SP,
        BLAKE2SP_128,
        BLAKE2SP_160,
        BLAKE2SP_224,
        BLAKE2SP_256,
        // BLAKE2XS
        BLAKE2XS,
        // BLAKE2B
        BLAKE2B,
        BLAKE2B_128,
        BLAKE2B_160,
        BLAKE2B_224,
        BLAKE2B_256,
        BLAKE2B_384,
        BLAKE2B_512,
        // BLAKE2BP
        BLAKE2BP,
        BLAKE2BP_128,
        BLAKE2BP_160,
        BLAKE2BP_224,
        BLAKE2BP_256,
        BLAKE2BP_384,
        BLAKE2BP_512,
        // BLAKE2XB
        BLAKE2XB,
        // BLAKE3
        BLAKE3,
        BLAKE3_128,
        BLAKE3_160,
        BLAKE3_224,
        BLAKE3_256,
        BLAKE3_384,
        BLAKE3_512,
        Unknown = -1,
    }

    public enum OutputType
    {
        BASE64,
        BinaryUpper,
        BinaryLower,
        Unknown = -1,
    }

    /// <summary>
    /// 选择导出每个文件的哪些算法的哈希结果
    /// </summary>
    public enum ExportAlgos
    {
        /// <summary>
        /// 导出每个文件当前显示的算法的结果
        /// </summary>
        Current,
        /// <summary>
        /// 导出每个文件所有已计算的算法的结果
        /// </summary>
        AllCalculated,
    }

    /// <summary>
    /// 哈希值结果可导出的文件类型
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// 导出为 .txt 文件，每行格式：[#算法名 *哈希值 *文件名]，不包括方括号
        /// </summary>
        TxtFile,
        /// <summary>
        /// 文件内容与 TxtFile 完全相同，仅文件扩展名不同，此类型为 .hcb 后缀<br/>
        /// 如果在设置面板中打开了本程序的文件关联，则可以直接通过双击 .hcb 文件打开本程序进行哈希校验<br/>
        /// 前提是<直接使用校验依据进行校验时>设置项选择了合适的文件搜索策略
        /// </summary>
        HcbFile,
    }

    /// <summary>
    /// 约定的 MemoryMappedFile 内容排布方案版本
    /// 默认约定：MemoryMappedFile 内的前 4 个字节总是代表内容排布方案版本，转为 int 值后强转为此枚举
    /// </summary>
    internal enum MappedVer
    {
        /// <summary>
        /// 未知版本，不读取 MemoryMappedFile 的内容
        /// </summary>
        Unknown,

        /// <summary>
        /// 版本 1，按版本 1 的规则读取 MemoryMappedFile 内容：<br/>
        /// a. 00~03(含) 字节：版本号，MemoryMappedFile 内容排布方案版本，所有版本都固定<br/>
        /// b. 04~07(含) 字节：内容数量(非字节数)，MemoryMappedFile 内容的条目的数量<br/>
        /// c. 08~11(含) 字节：第一个条目的字节数，表示这个条目从第 12 字节开始占用的字节数
        /// d. 12~n (含) 字节：第一个条目的内容，n 是 [8~11(含) 字节内容转为 int 值] + 11<br/>
        /// e. 下一个条目从第 n + 1 字节开始，以此类推......
        /// </summary>
        Version1,
    }

    /// <summary>
    /// 筛选器的筛选逻辑
    /// </summary>
    internal enum FilterLogic
    {
        /// <summary>
        /// 对象的目标属性(数组)里的任意一项符合筛选器多个要求里任意一个要求
        /// </summary>
        Any,

        /// <summary>
        /// 对象的目标属性(数组)涵盖了筛选器的所有要求，且不允许含有要求以外的项
        /// </summary>
        Strict,

        /// <summary>
        /// 对象的目标属性(数组)里的所有项都在筛选器多个要求组成的限制范围之内
        /// </summary>
        Within,

        /// <summary>
        /// 对象的目标属性(数组)涵盖了筛选器的所有要求，但也可能含有要求以外的项
        /// </summary>
        Cover,
    }

    internal enum XXH_errorcode
    {
        XXH_OK,
        XXH_ERROR
    }
}
