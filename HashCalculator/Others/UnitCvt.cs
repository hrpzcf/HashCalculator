namespace HashCalculator
{
    internal class UnitCvt
    {
        private const double kb = 1024D;
        private const double mb = 1048576D;
        private const double gb = 1073741824D;

        public static string FileSizeCvt(long bytes)
        {
            double bytesto;
            bytesto = bytes / gb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}GB";
            }
            bytesto = bytes / mb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}MB";
            }
            bytesto = bytes / kb;
            if (bytesto >= 1)
            {
                return $"{bytesto:f1}KB";
            }
            return $"{bytes}B";
        }
    }
}
