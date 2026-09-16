using System;

namespace QueueSimulator;

public enum TipoEvento
{
    Chegada,

    Saida,

    Passagem
}

public class Evento : IComparable<Evento>
{
    public TipoEvento Tipo { get; }

    public Fila Fila { get; }

    public Fila? Destino { get; }

    public double Tempo { get; }

    private Evento(TipoEvento tipo, double tempo, Fila fila, Fila? destino = null)
    {
        if (tempo < 0)
            throw new ArgumentException("An event cannot occur before time zero.");

        Tipo = tipo;
        Tempo = tempo;
        Fila = fila ?? throw new ArgumentNullException(nameof(fila));
        Destino = destino;
    }

    public static Evento Chegada(double tempo, Fila fila)
        => new(TipoEvento.Chegada, tempo, fila);

    public static Evento Saida(double tempo, Fila fila)
        => new(TipoEvento.Saida, tempo, fila);

    public static Evento Passagem(double tempo, Fila origem, Fila destino)
        => new(TipoEvento.Passagem, tempo, origem,
               destino ?? throw new ArgumentNullException(nameof(destino)));

    public int CompareTo(Evento? other)
        => other is null ? 1 : Tempo.CompareTo(other.Tempo);

    public override string ToString() => Tipo == TipoEvento.Passagem
        ? $"{Tipo} {Fila.Name} -> {Destino!.Name} @ {Tempo:F2}"
        : $"{Tipo} {Fila.Name} @ {Tempo:F2}";
}
