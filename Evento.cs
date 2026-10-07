using System;

namespace QueueSimulator;

public enum TipoEvento
{
    Chegada,

    Saida,

    Passagem
}

public class Evento
{
    public TipoEvento Tipo { get; }

    public int Origem { get; }

    public int Destino { get; }

    public double Tempo { get; }

    private Evento(TipoEvento tipo, double tempo, int origem, int destino)
    {
        if (tempo < 0)
            throw new ArgumentException("An event cannot occur before time zero.");

        bool valido = tipo switch
        {
            TipoEvento.Chegada  => origem == Rede.Exterior && destino >= 0,
            TipoEvento.Saida    => origem >= 0 && destino == Rede.Exterior,
            TipoEvento.Passagem => origem >= 0 && destino >= 0,
            _ => false
        };

        if (!valido)
            throw new ArgumentException($"Invalid origin/destination for a {tipo} event: {origem} -> {destino}.");

        Tipo = tipo;
        Tempo = tempo;
        Origem = origem;
        Destino = destino;
    }

    public static Evento Chegada(double tempo, int destino)
        => new(TipoEvento.Chegada, tempo, Rede.Exterior, destino);

    public static Evento Saida(double tempo, int origem)
        => new(TipoEvento.Saida, tempo, origem, Rede.Exterior);

    public static Evento Passagem(double tempo, int origem, int destino)
        => new(TipoEvento.Passagem, tempo, origem, destino);

    public override string ToString() => $"{Tipo} {Origem} -> {Destino} @ {Tempo:F2}";
}
