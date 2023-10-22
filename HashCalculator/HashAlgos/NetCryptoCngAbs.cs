using System;
using System.Security.Cryptography;

namespace HashCalculator
{
    internal abstract class NetCryptoCngAbs : HashAlgorithm, IHashAlgoInfo
    {
        private readonly HashAlgorithm algorithm;

        public NetCryptoCngAbs(HashAlgorithm algorithm)
        {
            this.algorithm = algorithm;
        }

        public abstract string AlgoName { get; }

        public abstract AlgoType AlgoType { get; }

        public abstract IHashAlgoInfo NewInstance();

        public override void Initialize()
        {
            this.algorithm.Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            this.algorithm.TransformBlock(array, ibStart, cbSize, null, 0);
        }

        protected override byte[] HashFinal()
        {
            this.algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return this.algorithm.Hash;
        }
    }
}
