using System;
using System.Collections.Generic;

public interface IGeradorAleatorio
{
    double NextDouble();
}

class RandomGen : IGeradorAleatorio
{
    public const uint SementePadrao = 98765;

    private uint x0;
    private const uint a = 1664525;
    private const uint c = 1013904223;
    private const long m = 4294967296L;

    public RandomGen(uint semente = SementePadrao)
    {
        x0 = semente;
    }

    public uint Next()
    {
        x0 = (uint)((long)a * x0 + c);
        return x0;
    }

    public double NextDouble() => (double)Next() / m;
}

// Fixed list of pseudo-random numbers given in the model file ("rndnumbers").
class ListaAleatorios : IGeradorAleatorio
{
    private readonly IReadOnlyList<double> numeros;
    private int proximo;

    public ListaAleatorios(IReadOnlyList<double> numeros)
    {
        foreach (double numero in numeros)
        {
            if (numero < 0 || numero >= 1)
                throw new ArgumentException($"Random number {numero} is outside [0, 1).");
        }

        this.numeros = numeros;
    }

    public double NextDouble()
        => proximo < numeros.Count
            ? numeros[proximo++]
            : throw new InvalidOperationException("The list of random numbers is exhausted.");
}
