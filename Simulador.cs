using System;

namespace QueueSimulator;

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

    public void AgendaChegadasIniciais()
    {
        foreach (Fila fila in rede.Filas)
        {
            if (fila.HasExternalArrivals())
                escalonador.Agenda(Evento.Chegada(rede.PrimeiraChegada, fila));
        }
    }

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

    private void AcumulaTempos(double tempoEvento)
    {
        double deltaT = tempoEvento - ultimoEvento;

        foreach (Fila fila in rede.Filas)
            fila.AccumulateTime(deltaT);

        ultimoEvento = tempoEvento;
    }

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

    public double Probabilidade(Fila fila, int estado)
        => TempoGlobal > 0 ? fila.TimeAt(estado) / TempoGlobal : 0.0;

    public void ProcessaChegada(Evento evento)
    {
        relogio = evento.Tempo;
        Fila fila = evento.Fila;

        bool servidorLivre = fila.HasFreeServer();

        bool aceito = !fila.IsFull();
        if (aceito)
            fila.In();
        else
            fila.Loss();

        AgendaProximaChegada(fila);

        if (aceito && servidorLivre)
            IniciaAtendimento(fila);
    }

    public void ProcessaPassagem(Evento evento)
    {
        if (evento.Tipo != TipoEvento.Passagem || evento.Destino is null)
            throw new ArgumentException("A Passagem event must carry a destination queue.");

        relogio = evento.Tempo;

        LiberaServidor(evento.Fila);

        Admite(evento.Destino);
    }

    public void ProcessaSaida(Evento evento)
    {
        if (evento.Tipo != TipoEvento.Saida)
            throw new ArgumentException("A Saida event must end a service that leaves the network.");

        relogio = evento.Tempo;

        LiberaServidor(evento.Fila);
    }

    private void LiberaServidor(Fila fila)
    {
        fila.Out();

        if (fila.Status() >= fila.Servers())
            IniciaAtendimento(fila);
    }

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
