namespace HashCalculator
{
    /// <summary>
    /// HashViewModel 的构造函数的参数，用于包装零散参数
    /// </summary>
    internal struct ModelArg
    {
        public string filepath;
        public string expected;
        public bool deprecated;

        public ModelArg(string[] hashpath)
        {
            this.filepath = hashpath[1];
            this.expected = hashpath[0];
            this.deprecated = false;
        }

        /// <summary>
        /// 约定：h 为空字符串则表示无法确定是否匹配
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="path"></param>
        public ModelArg(string hash, string path)
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
