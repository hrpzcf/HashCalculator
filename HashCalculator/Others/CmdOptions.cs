using System.Collections.Generic;
using CommandLine;

namespace HashCalculator
{
    internal interface IOptions
    {
        [Option('a', "algo", HelpText =
            "要使用的哈希算法名称（名称中的横杠替换为下划线），例如：SHA_1、SHA_256 等")]
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
}
