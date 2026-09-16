using System;

namespace QueueSimulator;

/// <summary>What happens to a client at a given instant.</summary>
public enum TipoEvento
{
    /// <summary>A client arrives from outside the network.</summary>
    Chegada,

    /// <summary>A client finishes service and leaves the network.</summary>
    Saida,

    /// <summary>A client finishes service in one queue and moves to another.</summary>
    Passagem
}

/// <summary>
/// One scheduled event: what happens, to which queue, and when.
/// Events are ordered by time only, so the scheduler always picks the
/// earliest one, no matter which queue it belongs to.
/// </summary>
public class Evento : IComparable<Evento>
{
    public TipoEvento Tipo { get; }

    /// <summary>Queue the event refers to. For Passagem, the queue being left.</summary>
    public Fila Fila { get; }

    /// <summary>Queue the client moves to. Only set for Passagem.</summary>
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

    /// <summary>
    /// Compares events by time only, as required by the assignment. This is
    /// the C# counterpart of Java's Comparable&lt;Evento&gt;.
    /// </summary>
    public int CompareTo(Evento? other)
        => other is null ? 1 : Tempo.CompareTo(other.Tempo);

    public override string ToString() => Tipo == TipoEvento.Passagem
        ? $"{Tipo} {Fila.Name} -> {Destino!.Name} @ {Tempo:F2}"
        : $"{Tipo} {Fila.Name} @ {Tempo:F2}";
}
