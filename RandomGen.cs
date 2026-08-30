class RandomGen
{
    private int x0 = 98765;
    private const int a = 1664525;
    private const int c = 1013904223;
    private const long m = 4294967296L;
    public RandomGen()
    {}

    public int Next()
    {
        long resultado = ((long) a * x0 + c) % m;
        x0 = (int)resultado;
        
        return x0;
    }

    public double NextDouble()
    {
        return (double)Next() / m;
    }
}