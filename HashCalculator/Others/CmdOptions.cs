using CommandLine;
using System.Collections.Generic;

namespace HashCalculator
{
    [Verb("compute")]
    internal class ComputeHash
    {
        [Option("algo")]
        public string Algo { get; set; }

        [Value(0)]
        public IEnumerable<string> FilePaths { get; set; }
    }

    [Verb("verify")]
    internal class VerifyHash
    {
        [Option("algo", Default = -1)]
        public int AlgoEnum { get; set; }

        [Option("basis", Required = true)]
        public int BasisPath { get; set; }
    }
}
