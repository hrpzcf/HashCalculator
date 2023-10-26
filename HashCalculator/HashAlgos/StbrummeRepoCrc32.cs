using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal class StbrummeRepoCrc32 : HashAlgorithm, IHashAlgoInfo
    {
        private uint previousCrc32 = 0u;

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint crc32_16bytes(byte[] input, ulong in_len, uint prevCrc32);

        [DllImport(Embedded.Hashes, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint crc32_16bytes(ref byte input, ulong in_len, uint prevCrc32);

        public string AlgoName => "Crc32";

        public AlgoType AlgoType => AlgoType.CRC32;

        public override void Initialize()
        {
            this.previousCrc32 = 0u;
        }

        public IHashAlgoInfo NewInstance()
        {
            return new StbrummeRepoCrc32();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (ibStart == 0 && cbSize == array.Length)
            {
                this.previousCrc32 = crc32_16bytes(array, (ulong)cbSize, this.previousCrc32);
            }
            else
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(array, ibStart, cbSize);
                ref byte input = ref MemoryMarshal.GetReference(span);
                this.previousCrc32 = crc32_16bytes(ref input, (ulong)cbSize, this.previousCrc32);
            }
        }

        protected override byte[] HashFinal()
        {
            byte[] resultBuffer = BitConverter.GetBytes(this.previousCrc32);
            Array.Reverse(resultBuffer);
            return resultBuffer;
        }
    }
}
