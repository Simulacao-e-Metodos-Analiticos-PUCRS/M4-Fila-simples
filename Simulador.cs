using System;
using System.Collections.Generic;
using System.Linq;

namespace QueueSimulator;

public record ResultadoFila(double[] Tempos, double Perdas);

public record ResultadoSimulacao(double TempoGlobal, double Eventos, double Aleatorios, IReadOnlyList<ResultadoFila> Filas)
{
    // Mean of several runs of the same network: accumulated times, losses and global time.
    public static ResultadoSimulacao Media(IReadOnlyList<ResultadoSimulacao> execucoes)
    {
        int n = execucoes.Count;
        var filas = new List<ResultadoFila>();

        for (int fila = 0; fila < execucoes[0].Filas.Count; fila++)
        {
            int estados = execucoes.Max(execucao => execucao.Filas[fila].Tempos.Length);
            double[] tempos = new double[estados];

            foreach (ResultadoSimulacao execucao in execucoes)
            {
                double[] origem = execucao.Filas[fila].Tempos;
                for (int estado = 0; estado < origem.Length; estado++)
                    tempos[estado] += origem[estado] / n;
            }

            filas.Add(new ResultadoFila(tempos, execucoes.Average(execucao => execucao.Filas[fila].Perdas)));
        }

        return new ResultadoSimulacao(
            execucoes.Average(execucao => execucao.TempoGlobal),
            execucoes.Average(execucao => execucao.Eventos),
            execucoes.Average(execucao => execucao.Aleatorios),
            filas);
    }
}

public class Simulador
{
    private readonly uint maxAleatorios;

    private readonly Rede rede;
    private readonly Escalonador escalonador = new();
    private readonly IGeradorAleatorio gerador;

    private double relogio;
    private double ultimoEvento;
    private uint aleatoriosUsados;
    private bool encerrada;
    private uint eventosProcessados;

    public Simulador(Rede rede, IGeradorAleatorio gerador, uint maxAleatorios = Modelo.AleatoriosPadrao)
    {
        this.rede = rede ?? throw new ArgumentNullException(nameof(rede));
        this.gerador = gerador ?? throw new ArgumentNullException(nameof(gerador));
        this.maxAleatorios = maxAleatorios;
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
            if (rede.PrimeiraChegadaDe(fila.Indice) is double primeiraChegada)
                escalonador.Agenda(Evento.Chegada(primeiraChegada, fila.Indice));
        }
    }

    public void Executa(bool debug = false)
    {
        if (debug)
            ConsolePrinter.PrintNetworkDebugHeader(rede);

        AgendaChegadasIniciais();

        while (escalonador.TemEventos() && !encerrada)
        {
            Evento evento = escalonador.Proximo();
            Processa(evento);
            eventosProcessados++;

            if (debug)
                ConsolePrinter.PrintNetworkDebugEvent(eventosProcessados, evento, rede);

            if (aleatoriosUsados >= maxAleatorios)
                encerrada = true;
        }

        VerificaTempos();
        VerificaProbabilidades();
    }

    public ResultadoSimulacao Resultado()
        => new(TempoGlobal, eventosProcessados, aleatoriosUsados,
               rede.Filas.Select(fila => new ResultadoFila((double[])fila.Times().Clone(), fila.Losses())).ToList());

    private void Processa(Evento evento)
    {
        AcumulaTempos(evento.Tempo);
        relogio = evento.Tempo;

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

    // Origin is the outside (-1); destination is the queue that receives the client.
    private void ProcessaChegada(Evento evento)
    {
        Fila fila = rede.FilaEm(evento.Destino);

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

    // Works for any pair of queues, including a queue routing to itself:
    // the origin server is released before the client is admitted again.
    private void ProcessaPassagem(Evento evento)
    {
        LiberaServidor(rede.FilaEm(evento.Origem));
        Admite(rede.FilaEm(evento.Destino));
    }

    // Origin is the queue that finished the service; destination is the outside (-1).
    private void ProcessaSaida(Evento evento)
        => LiberaServidor(rede.FilaEm(evento.Origem));

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
        escalonador.Agenda(Evento.Chegada(relogio + intervalo, fila.Indice));
    }

    // Draws the service time and then the routing of the client that will leave the
    // server, so the end-of-service event is created already knowing its destination.
    private void IniciaAtendimento(Fila fila)
    {
        double u = ProximoAleatorio();
        if (encerrada) return;

        double fimAtendimento = relogio + fila.MinService() + u * (fila.MaxService() - fila.MinService());

        int destino = SorteiaDestino(fila);
        if (encerrada) return;

        escalonador.Agenda(destino == Rede.Exterior
            ? Evento.Saida(fimAtendimento, fila.Indice)
            : Evento.Passagem(fimAtendimento, fila.Indice, destino));
    }

    // A single route has probability 1 and needs no random number.
    private int SorteiaDestino(Fila fila)
        => rede.RoteamentoDeterministico(fila.Indice)
            ? rede.Destino(fila.Indice, 0.0)
            : rede.Destino(fila.Indice, ProximoAleatorio());

    private double ProximoAleatorio()
    {
        if (aleatoriosUsados >= maxAleatorios)
        {
            encerrada = true;
            return 0;
        }

        aleatoriosUsados++;
        return gerador.NextDouble();
    }
}
