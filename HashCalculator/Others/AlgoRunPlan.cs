using System.Collections.Generic;

namespace HashCalculator.Others;

/// <summary>
/// 本轮计算的运行计划：由 HashViewModel.BuildRunPlan 依据计算意图产出，再由其应用到算法清单。<br/>
/// 只描述"清单去向"与"本轮算哪些"，不含任何对清单的操作（操作由清单拥有者执行）。
/// </summary>
internal readonly struct AlgoRunPlan
{
    /// <summary>
    /// 是否丢弃现有清单、按当前设置的勾选算法重建（重建后全部计算）。
    /// </summary>
    public bool RebuildList { get; init; }

    /// <summary>
    /// 本轮要计算的算法类型；为 null 表示清单内全部算法。
    /// </summary>
    public HashSet<AlgoType> TypesToHash { get; init; }
}
