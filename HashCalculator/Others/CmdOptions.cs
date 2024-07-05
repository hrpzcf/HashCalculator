using System.Collections.Generic;
using CommandLine;

namespace HashCalculator
{
    internal interface IOptions
    {
        [Option('a', "algo",
            HelpText = "要使用的哈希算法名称（名称中的横杠替换为下划线），如：SHA_1")]
        string Algos { get; set; }
    }

    [Verb("verify")]
    internal class VerifyHash : IOptions
    {
        public string Algos { get; set; }

        [Option('l', "list", Required = true)]
        public string ChecklistPath { get; set; }
    }

    [Verb("compute")]
    internal class ComputeHash : IOptions
    {
        public string Algos { get; set; }

        [Value(0, Min = 1, Required = true)]
        public IEnumerable<string> FilePaths { get; set; }
    }

    [Verb("shell")]
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
