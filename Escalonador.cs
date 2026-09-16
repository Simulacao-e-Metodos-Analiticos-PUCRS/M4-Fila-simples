using System;
using System.Collections.Generic;

namespace QueueSimulator;

public class Escalonador
{
    private readonly PriorityQueue<Evento, double> eventos = new();

    public int Pendentes => eventos.Count;

    public void Agenda(Evento evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        eventos.Enqueue(evento, evento.Tempo);
    }

    public bool TemEventos() => eventos.Count > 0;

    public Evento Proximo()
        => eventos.Count > 0
            ? eventos.Dequeue()
            : throw new InvalidOperationException("There are no scheduled events.");

    public Evento Espia()
        => eventos.Count > 0
            ? eventos.Peek()
            : throw new InvalidOperationException("There are no scheduled events.");
}
