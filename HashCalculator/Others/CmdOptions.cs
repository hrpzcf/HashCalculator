using System.Collections.Generic;
using CommandLine;

namespace HashCalculator
{
    internal interface IOptions
    {
        [Option('a', "algo", HelpText = "要使用的哈希算法名称（名称中的横杠替换为下划线），如：SHA_1")]
        string Algos { get; set; }
    }

    [Verb("verify", HelpText = "用预期的哈希值清单来校验目标文件的哈希值是否与预期相符。")]
    internal class VerifyHash : IOptions
    {
        public const string Verb = "verify";
        public const string Checklist = "--list";

        public string Algos { get; set; }

        [Option('l', "list", Required = true)]
        public string ChecklistPath { get; set; }
    }

    [Verb("compute", HelpText = "用指定算法计算目标文件、文件夹内的文件的哈希值。")]
    internal class ComputeHash : IOptions
    {
        public const string Verb = "compute";

        public string Algos { get; set; }

        [Value(0, Min = 1, Required = true)]
        public IEnumerable<string> FilePaths { get; set; }
    }

    [Verb("shell", HelpText = "安装或卸载 HashCalculator 的系统右键菜单。")]
    internal class ShellInstallation : IOptions
    {
        // 本类实现 IOptions 接口并没有实际用处
        public string Algos { get; set; }

        [Option('s', "silent")]
        public bool InstallSilently { get; set; }

        [Option('i', "install", SetName = "installation")]
        public bool Install { get; set; }

        [Option('u', "uninstall", SetName = "installation")]
        public bool Uninstall { get; set; }
    }
}
