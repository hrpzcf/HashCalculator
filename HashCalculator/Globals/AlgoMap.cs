using System.Collections.Generic;

namespace HashCalculator
{
    internal static class AlgoMap
    {
        private static readonly Dictionary<AlgoType, string> algoMap =
            new Dictionary<AlgoType, string>()
            {
                { AlgoType.SHA1, "SHA-1" },
                { AlgoType.SHA224, "SHA-224"},
                { AlgoType.SHA256, "SHA-256"},
                { AlgoType.SHA384, "SHA-384"},
                { AlgoType.SHA512, "SHA-512"},
                { AlgoType.SHA3_224, "SHA3-224"},
                { AlgoType.SHA3_256, "SHA3-256"},
                { AlgoType.SHA3_384, "SHA3-384"},
                { AlgoType.SHA3_512, "SHA3-512"},
                { AlgoType.MD5, "MD5"},
                { AlgoType.BLAKE2b, "BLAKE2b"},
                { AlgoType.BLAKE2s, "BLAKE2s"},
                { AlgoType.BLAKE3, "BLAKE3"},
                { AlgoType.Whirlpool, "Whirlpool"},
                { AlgoType.Unknown, "- N/A -"},
            };

        public static string GetAlgoName(AlgoType algoType)
        {
            if (algoMap.TryGetValue(algoType, out string name))
            {
                return name;
            }
            return string.Empty;
        }

        public static AlgoType GetAlgoType(string name)
        {
            if (name is null)
            {
                return AlgoType.Unknown;
            }
            foreach (KeyValuePair<AlgoType, string> keyValue in algoMap)
            {
                if (keyValue.Value == name)
                {
                    return keyValue.Key;
                }
            }
            return AlgoType.Unknown;
        }
    }
}
