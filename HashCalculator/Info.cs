using System;
using System.Reflection;
using System.Windows;

namespace HashCalculator
{
    internal static class Info
    {
        private static readonly AssemblyName assembly = Application.ResourceAssembly.GetName();
        private static readonly Version v = assembly.Version;

        public static readonly string AppName = assembly.Name;
        public static readonly string Ver = $"{v.Major}.{v.Minor}.{v.Build}";
        public const string Title = "文件哈希值批量计算器";
        public const string Author = "hrpzcf";
        public const string Source = "https://github.com/hrpzcf/HashCalculator";
        public const string Published = "www.52pojie.cn";
    }
}
