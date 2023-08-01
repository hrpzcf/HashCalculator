namespace HashCalculator
{
    internal static class BufferSize
    {
        private const long maxTimes = 1024L;
        private const long pageSize = 4096L;

        /// <summary>
        /// 4MB 内小文件尽量一次全部读取
        /// </summary>
        public static int Suggest(long fileSize)
        {
            long timesToPageSize = fileSize / pageSize + 1L;
            if (timesToPageSize > maxTimes)
            {
                timesToPageSize = maxTimes;
            }
            return (int)(pageSize * timesToPageSize);
        }
    }
}
