namespace HashCalculator
{
    internal static class BufferSize
    {
        /// <summary>
        /// 2MB 内小文件尽量一次全部读取
        /// </summary>
        public static int Suggest(long fileSize)
        {
            int times = (int)(fileSize / pageSize) + 1;
            if (times > 512)
            {
                times = 512;
            }
            return pageSize * times;
        }

        private const int pageSize = 4096;
    }
}
