using System;
using System.Collections.Generic;

namespace QueueSimulator;

/// <summary>
/// Minimum priority queue of events. Always hands back the earliest
/// scheduled event, no matter which type it is or which queue it belongs to.
/// </summary>
public class Escalonador
{
    private readonly PriorityQueue<Evento, double> eventos = new();

    /// <summary>How many events are still scheduled.</summary>
    public int Pendentes => eventos.Count;

    /// <summary>Schedules an event, keyed by the instant it happens.</summary>
    public void Agenda(Evento evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        eventos.Enqueue(evento, evento.Tempo);
    }

    /// <summary>True while there is any event left to process.</summary>
    public bool TemEventos() => eventos.Count > 0;

    /// <summary>Removes and returns the earliest scheduled event.</summary>
    public Evento Proximo()
        => eventos.Count > 0
            ? eventos.Dequeue()
            : throw new InvalidOperationException("There are no scheduled events.");

    /// <summary>Returns the earliest scheduled event without removing it.</summary>
    public Evento Espia()
        => eventos.Count > 0
            ? eventos.Peek()
            : throw new InvalidOperationException("There are no scheduled events.");
}
