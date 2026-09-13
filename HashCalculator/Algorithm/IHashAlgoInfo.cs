namespace HashCalculator;

public interface IHashAlgoInfo
{
    string AlgoName { get; }

    AlgoType AlgoType { get; }

    int DigestLength { get; }

    /// <summary>
    /// 释放持有的一次性非托管状态（如 dll 句柄），之后仍可 Initialize 复用。<br/>
    /// 与 HashAlgorithm.Dispose 的区别：后者只用于"弃用实例"，会让实例永久不可再用；
    /// 本方法只清理资源，供每轮计算结束后调用。<br/>
    /// 无一次性非托管状态的算法应显式实现为空方法，以保持各算法实现一致。
    /// </summary>
    void Release();

    IHashAlgoInfo NewInstance();
}
