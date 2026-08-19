using System.Collections.Generic;
using System.CommandLine;

namespace HashCalculator;

internal static class CmdOptions
{
    public const string ShellExtVerb = "shell";
    public const string ComputeHashVerb = "compute";
    public const string CheckHashVerb = "verify";
    public const string ChecklistArgLong = "--list";
    public const string ChecklistArgShort = "-l";

    /// <summary>
    /// compute 和 verify 共用的 -a/--algo 选项
    /// </summary>
    public static readonly Option<string> AlgoOption =
        new Option<string>("--algo", "-a")
        {
            Description = "要使用的哈希算法名称（名称中的横杠替换为下划线），如：SHA_1。",
        };

    /// <summary>
    /// compute 子命令：计算目标文件、文件夹内的文件的哈希值
    /// </summary>
    public static readonly Argument<IEnumerable<string>> PathsToCompute =
        new Argument<IEnumerable<string>>("paths")
        {
            Arity = ArgumentArity.OneOrMore,
        };
    public static readonly Command ComputeCommand =
        new Command(ComputeHashVerb, "用指定算法计算目标文件、文件夹内的文件的哈希值。")
        {
            AlgoOption,
            PathsToCompute,
        };

    /// <summary>
    /// verify 子命令：用校验信息文件来校验目标文件的哈希值是否与预期相符
    /// </summary>
    public static readonly Option<string> CheckListOption =
        new Option<string>(ChecklistArgLong, ChecklistArgShort)
        {
            Required = true,
        };
    /// <summary>
    /// 用于吸收 verify 命令多余的位置参数（如误输入的 token），避免触发"未识别命令或参数"错误
    /// </summary>
    public static readonly Argument<IEnumerable<string>> ExtraArguments =
        new Argument<IEnumerable<string>>("extra")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };
    public static readonly Command CheckHashCommand =
        new Command(CheckHashVerb, "用校验信息文件来校验目标文件的哈希值是否与预期相符。")
        {
            AlgoOption,
            CheckListOption,
            ExtraArguments,
        };

    /// <summary>
    /// shell 子命令：安装或卸载 HashCalculator 的系统右键菜单
    /// </summary>
    public static readonly Option<bool> SilentOption = new Option<bool>("--silent", "-s");
    public static readonly Option<bool> InstallOption = new Option<bool>("--install", "-i");
    public static readonly Option<bool> UninstallOption = new Option<bool>("--uninstall", "-u");
    public static readonly Command ShellExtCommand =
        new Command(ShellExtVerb, "安装或卸载 HashCalculator 的系统右键菜单扩展模块。")
        {
            InstallOption,
            UninstallOption,
            SilentOption,
        };

    /// <summary>
    /// 根命令：挂载 compute、verify、shell 三个子命令
    /// </summary>
    public static readonly RootCommand RootCommand = new RootCommand("HashCalculator")
    {
        ComputeCommand,
        CheckHashCommand,
        ShellExtCommand,
    };
}
