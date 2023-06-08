using System;
using System.Runtime.InteropServices;

namespace HashCalculator
{
    internal static class NativeFunctions
    {
        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems
        /// </summary>
        /// <param name="pidlFolder"></param>
        /// <param name="cidl"></param>
        /// <param name="apidl"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/zh-cn/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bindingContext"></param>
        /// <param name="pidl"></param>
        /// <param name="sfgaoIn"></param>
        /// <param name="psfgaoOut"></param>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern void SHParseDisplayName(
            string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);
    }
}
