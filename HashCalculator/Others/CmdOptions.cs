using CommandLine;
using System.Collections.Generic;

namespace HashCalculator
{
    [Verb("compute")]
    internal class ComputeHash
    {
        [Option('a', "algo", HelpText = "要使用的哈希算法，可以是数字或算法名称，例如：1(代表算法列表第一个算法 sha1)、sha3_256 等")]
        public string Algo { get; set; }

        [Value(0, Required = true)]
        public IEnumerable<string> FilePaths { get; set; }
    }

    [Verb("verify")]
    internal class VerifyHash
    {
        [Option('a', "algo", HelpText = "要使用的哈希算法，可以是数字或算法名称，例如：1(代表算法列表第一个算法 sha1)、sha3_256 等")]
        public string Algo { get; set; }

        [Option('b', "basis", Required = true)]
        public string BasisPath { get; set; }
    }
}
