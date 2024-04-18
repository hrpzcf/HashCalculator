namespace HashCalculator
{
    public interface IHashAlgoInfo
    {
        string AlgoName { get; }

        AlgoType AlgoType { get; }

        int DigestLength { get; }

        IHashAlgoInfo NewInstance();
    }
}
