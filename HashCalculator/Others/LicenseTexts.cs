using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace HashCalculator
{
    /// <summary>
    /// 读取嵌入在程序集中的许可全文。<br/>
    /// 许可原文不写进代码，而是编译时以 EmbeddedResource 嵌入，
    /// 保证文本与上游许可文件逐字节一致，单文件发布时也不依赖外部文件。
    /// </summary>
    internal static class LicenseTexts
    {
        private const string ResourceNamePrefix = "HashCalculator.Licenses.";

        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        /// <summary>
        /// 读取指定许可全文，并按资源名缓存，避免重复读取。
        /// 资源不存在或读取失败时返回可直接展示的提示文本，不抛出异常。
        /// </summary>
        /// <param name="resourceName">许可资源名后缀，如 LICENSE.blake2。</param>
        internal static string Get(string resourceName)
        {
            if (_cache.TryGetValue(resourceName, out string cached))
            {
                return cached;
            }
            string text = Load(resourceName);
            _cache[resourceName] = text;
            return text;
        }

        private static string Load(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(
                    ResourceNamePrefix + resourceName);
                if (stream == null)
                {
                    return $"未找到许可全文资源（{resourceName}）。";
                }
                using StreamReader reader = new StreamReader(
                    stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"读取许可全文失败：{ex.Message}";
            }
        }
    }
}
