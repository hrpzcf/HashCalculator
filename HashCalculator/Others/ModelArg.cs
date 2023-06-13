namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于打包零散参数
    /// </summary>
    internal class ModelArg
    {
        public readonly string filepath;
        public readonly byte[] expected;
        public readonly bool deprecated;

        /// <summary>
        /// 约定：hash 为空字符串则表示无法确定是否匹配
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        public ModelArg(byte[] hash, string path)
        {
            this.filepath = path;
            this.expected = hash;
            this.deprecated = false;
        }

        public ModelArg(string path)
        {
            this.filepath = path;
            this.expected = null;
            this.deprecated = false;
        }

        public ModelArg(string path, bool deprecated)
        {
            this.filepath = path;
            this.expected = null;
            this.deprecated = deprecated;
        }
    }
}
