namespace HashCalculator
{
    public interface IHashAlgoInfo
    {
        string AlgoName { get; }

        AlgoType AlgoType { get; }

        IHashAlgoInfo NewInstance();
    }
}
