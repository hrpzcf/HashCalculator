using System;
using System.Reflection;

namespace HashCalculator
{
    internal static class Info
    {
        public const string AppGuid = "{8CD5CD37-5AA1-492A-A208-7E9BBDDC88CC}";
        public const string ProcIdGuid = "{DA4EEDE8-5767-472F-9A40-1B90D7097953}";
        public const string MappedGuid = "{EF36065E-10F3-4ABA-A66F-E68A42A8DA7D}";
        public const string TypeLibGuid = "{18D6B7F2-F466-481F-ADFC-849B5F9FBD0B}";
        public const string RegGuidComputeHash = "{DE95CAC8-90D1-4C55-B81D-D7F6D527606C}";
        public const string RegGuidAsCheckList = "{50B22DF9-3FF8-428E-900F-F6EE89F1A18B}";

        public static readonly AssemblyName Executing = App.Executing.GetName();
        public static readonly string Name = Executing.Name;
        public static readonly Version V = Executing.Version;
        public static readonly string Ver = $"{V.Major}.{V.Minor}.{V.Build}{(V.Revision > 0 ? $"-preview{V.Revision}" : "")}";

        public const string Title = "哈希值批量计算器";
        public const string Author = "hrpzcf";
        public const string Website = "www.52pojie.cn";
        public const string Source = "https://github.com/hrpzcf/HashCalculator";

        // 兼容的 Shell 扩展版本上下限，包含下限，但不包含上限
        public static readonly Version LowerLimitOfShellExtVersion = new Version("5.24.0");
        public static readonly Version UpperLimitOfShellExtVersion = new Version("6.1.0.0");
    }
}
