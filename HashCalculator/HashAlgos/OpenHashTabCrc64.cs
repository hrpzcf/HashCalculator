using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class OpenHashTabCrc64 : HashAlgorithm, IHashAlgoInfo
    {
        private ulong previousCrc64 = 0ul;

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong crc64_update(ulong prevCrc32, byte[] input, ulong in_len);

        [DllImport(Embedded.HashAlgs, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong crc64_update(ulong prevCrc32, ref byte input, ulong in_len);

        public string AlgoName => "Crc64";

        public AlgoType AlgoType => AlgoType.CRC64;

        public override void Initialize()
        {
            this.previousCrc64 = 0ul;
        }

        public IHashAlgoInfo NewInstance()
        {
            return new OpenHashTabCrc64();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (ibStart == 0 && cbSize == array.Length)
            {
                this.previousCrc64 = crc64_update(this.previousCrc64, array, (ulong)cbSize);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this.previousCrc64 = crc64_update(this.previousCrc64, ref input, (ulong)cbSize);
            }
        }

        protected override byte[] HashFinal()
        {
            byte[] resultBuffer = BitConverter.GetBytes(this.previousCrc64);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
