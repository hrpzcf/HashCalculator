using System;
using System.Reflection;
using System.Windows;

namespace HashCalculator
{
    internal static class Info
    {
        private static readonly AssemblyName assembly = Application.ResourceAssembly.GetName();

        public const string Title = "文件哈希值批量计算器";
        public static readonly Version Version = assembly.Version;
        public const string Author = "hrpzcf";
        public const string Source = "https://github.com/hrpzcf/HashCalculator";
        public const string Published = "www.52pojie.cn";
        public static readonly string AppName = assembly.Name;
    }
}
