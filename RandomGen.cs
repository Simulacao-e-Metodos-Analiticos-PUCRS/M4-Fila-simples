class RandomGen
{
    private uint x0 = 98765;
    private const uint a = 1664525;
    private const uint c = 1013904223;
    private const long m = 4294967296L;
    public RandomGen()
    {}

    public uint Next()
    {
        x0 = (uint)((long)a * x0 + c);
        return x0;
    }

    public double NextDouble() => (double)Next() / m;
}