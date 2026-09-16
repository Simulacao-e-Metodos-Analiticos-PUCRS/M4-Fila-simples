using System;

namespace QueueSimulator;

/// <summary>
/// Event driven simulation of a queueing network. Holds the state that is
/// global to a run: the clock, the scheduler and the random number budget.
/// Everything that belongs to a single queue lives in <see cref="Fila"/>.
/// </summary>
public class Simulador
{
    private const uint MaxAleatorios = 100_000;

    private readonly Rede rede;
    private readonly Escalonador escalonador = new();
    private readonly RandomGen gerador = new();

    private double relogio;
    private double ultimoEvento;
    private uint aleatoriosUsados;
    private bool encerrada;
    private uint eventosProcessados;

    public Simulador(Rede rede)
    {
        this.rede = rede ?? throw new ArgumentNullException(nameof(rede));
    }

    public double Relogio => relogio;
    public double TempoGlobal => relogio;
    public uint AleatoriosUsados => aleatoriosUsados;
    public bool Encerrada => encerrada;
    public Escalonador Escalonador => escalonador;
    public uint EventosProcessados => eventosProcessados;

    /// <summary>
    /// Schedules the first external arrival. Only queues fed from outside the
    /// network get one, so a queue like Fila 2 never sees a Chegada event.
    /// </summary>
    public void AgendaChegadasIniciais()
    {
        foreach (Fila fila in rede.Filas)
        {
            if (fila.HasExternalArrivals())
                escalonador.Agenda(Evento.Chegada(rede.PrimeiraChegada, fila));
        }
    }

    /// <summary>
    /// Runs the whole simulation: starts the external arrival streams and
    /// processes events in chronological order until the random number budget
    /// is spent. The run ends as soon as the last allowed random is used, so
    /// the event that used it is the last one accounted for.
    /// </summary>
    public void Executa()
    {
        AgendaChegadasIniciais();

        while (escalonador.TemEventos() && !encerrada)
        {
            Processa(escalonador.Proximo());
            eventosProcessados++;

            if (aleatoriosUsados >= MaxAleatorios)
                encerrada = true;
        }

        VerificaTempos();
        VerificaProbabilidades();
    }

    public void ImprimeResultados()
        => ConsolePrinter.PrintNetworkResults(rede, TempoGlobal, eventosProcessados, aleatoriosUsados);

    /// <summary>
    /// Accounts for the time elapsed since the previous event and then hands
    /// the event to the handler for its type.
    /// </summary>
    public void Processa(Evento evento)
    {
        AcumulaTempos(evento.Tempo);

        switch (evento.Tipo)
        {
            case TipoEvento.Chegada:  ProcessaChegada(evento);  break;
            case TipoEvento.Passagem: ProcessaPassagem(evento); break;
            case TipoEvento.Saida:    ProcessaSaida(evento);    break;
        }
    }

    /// <summary>
    /// Credits the interval since the previous event to the state every queue
    /// is in. Runs before the event is handled, while the queues still hold
    /// the population they had during that interval.
    /// </summary>
    private void AcumulaTempos(double tempoEvento)
    {
        double deltaT = tempoEvento - ultimoEvento;

        foreach (Fila fila in rede.Filas)
            fila.AccumulateTime(deltaT);

        ultimoEvento = tempoEvento;
    }

    /// <summary>
    /// Every queue must account for the whole simulated time: the times of its
    /// states have to add up to the global clock. Throws when they do not.
    /// </summary>
    public void VerificaTempos(double tolerancia = 1e-6)
    {
        foreach (Fila fila in rede.Filas)
        {
            double soma = fila.Times().Sum();

            if (Math.Abs(soma - TempoGlobal) > tolerancia)
                throw new InvalidOperationException(
                    $"[{fila.Name}] accumulated {soma:F6} minutes, but the global time is {TempoGlobal:F6}.");
        }
    }

    /// <summary>
    /// The state probabilities of every queue must add up to one, since a
    /// queue is always in exactly one of its states. Throws when they do not.
    /// </summary>
    public void VerificaProbabilidades(double tolerancia = 1e-9)
    {
        foreach (Fila fila in rede.Filas)
        {
            double soma = 0.0;

            for (int estado = 0; estado < fila.StateCount(); estado++)
                soma += Probabilidade(fila, estado);

            if (Math.Abs(soma - 1.0) > tolerancia)
                throw new InvalidOperationException(
                    $"[{fila.Name}] state probabilities add up to {soma:F12}, not to 1.");
        }
    }

    /// <summary>P(state i) = time accumulated in state i / global time.</summary>
    public double Probabilidade(Fila fila, int estado)
        => TempoGlobal > 0 ? fila.TimeAt(estado) / TempoGlobal : 0.0;

    /// <summary>
    /// A client arrives from outside the network: it is admitted or lost, the
    /// next external arrival is scheduled, and service starts if a server was
    /// free when the client showed up.
    /// </summary>
    public void ProcessaChegada(Evento evento)
    {
        relogio = evento.Tempo;
        Fila fila = evento.Fila;

        // Whether this client is served right away depends on the occupation
        // *before* it is admitted, since In() already makes it count as served.
        bool servidorLivre = fila.HasFreeServer();

        // 1-3. Respect the capacity: admit the client, or record a loss.
        bool aceito = !fila.IsFull();
        if (aceito)
            fila.In();
        else
            fila.Loss();

        // 4. The arrival stream goes on whether or not this client got in.
        AgendaProximaChegada(fila);

        // 5. An admitted client only waits if every server is busy.
        if (aceito && servidorLivre)
            IniciaAtendimento(fila);
    }

    /// <summary>
    /// A client finishes service in one queue and moves to another. The origin
    /// frees a server and takes its next waiting client; the destination admits
    /// the arriving client, or records a loss when it is full.
    /// </summary>
    public void ProcessaPassagem(Evento evento)
    {
        if (evento.Tipo != TipoEvento.Passagem || evento.Destino is null)
            throw new ArgumentException("A Passagem event must carry a destination queue.");

        relogio = evento.Tempo;

        // 1-3. The client leaves and the freed server takes whoever was waiting.
        LiberaServidor(evento.Fila);

        // 4-5. The client does not leave the system: it joins the next queue,
        // which may turn it away if it is full.
        Admite(evento.Destino);
    }

    /// <summary>
    /// A client finishes service in the last queue of its path and leaves the
    /// network for good. The freed server takes the next client waiting, if any.
    /// </summary>
    public void ProcessaSaida(Evento evento)
    {
        if (evento.Tipo != TipoEvento.Saida)
            throw new ArgumentException("A Saida event must end a service that leaves the network.");

        relogio = evento.Tempo;

        // 1-3. The client is removed and the freed server takes the next in line.
        // 4. Nothing else follows: this queue routes to no other one, so the
        //    client is simply gone.
        LiberaServidor(evento.Fila);
    }

    /// <summary>
    /// Removes the client that has just been served. The server it was using
    /// becomes free, and immediately takes the next client in line, if any.
    /// </summary>
    private void LiberaServidor(Fila fila)
    {
        fila.Out();

        // After Out(), a client was waiting exactly when the queue still holds
        // at least as many clients as it has servers.
        if (fila.Status() >= fila.Servers())
            IniciaAtendimento(fila);
    }

    /// <summary>
    /// Admits a client coming from another queue. A full queue drops the
    /// client and counts the loss against itself.
    /// </summary>
    private void Admite(Fila fila)
    {
        bool servidorLivre = fila.HasFreeServer();

        if (fila.IsFull())
        {
            fila.Loss();
            return;
        }

        fila.In();

        if (servidorLivre)
            IniciaAtendimento(fila);
    }

    private void AgendaProximaChegada(Fila fila)
    {
        if (!fila.HasExternalArrivals())
            return;

        double u = ProximoAleatorio();
        if (encerrada) return;

        double intervalo = fila.MinArrival() + u * (fila.MaxArrival() - fila.MinArrival());
        escalonador.Agenda(Evento.Chegada(relogio + intervalo, fila));
    }

    private void IniciaAtendimento(Fila fila)
    {
        double u = ProximoAleatorio();
        if (encerrada) return;

        double atendimento = fila.MinService() + u * (fila.MaxService() - fila.MinService());
        escalonador.Agenda(FimDeAtendimento(fila, relogio + atendimento));
    }

    /// <summary>
    /// Builds the event that ends a service: a Passagem when the client moves
    /// on to another queue, a Saida when it leaves the network. Routing only
    /// costs a random number when the queue has more than one destination.
    /// </summary>
    private Evento FimDeAtendimento(Fila fila, double tempo)
    {
        Fila? destino = rede.RoteamentoDeterministico(fila)
            ? rede.Destino(fila)
            : rede.Destino(fila, ProximoAleatorio());

        return destino is null
            ? Evento.Saida(tempo, fila)
            : Evento.Passagem(tempo, fila, destino);
    }

    private double ProximoAleatorio()
    {
        if (aleatoriosUsados >= MaxAleatorios)
        {
            encerrada = true;
            return 0;
        }

        aleatoriosUsados++;
        return gerador.NextDouble();
    }
}
