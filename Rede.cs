using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QueueSimulator;

/// <summary>Where a client goes after being served, and how often.</summary>
public class Rota
{
    /// <summary>Destination queue, or null when the client leaves the network.</summary>
    public Fila? Destino { get; }

    public double Probabilidade { get; }

    public Rota(Fila? destino, double probabilidade)
    {
        if (probabilidade <= 0 || probabilidade > 1)
            throw new ArgumentException("A route probability must be within (0, 1].");

        Destino = destino;
        Probabilidade = probabilidade;
    }
}

/// <summary>
/// The queueing network: its queues, how clients are routed between them,
/// and when the first external client arrives.
/// </summary>
public class Rede
{
    private readonly List<Fila> filas;
    private readonly Dictionary<string, List<Rota>> rotas;

    public IReadOnlyList<Fila> Filas => filas;

    public double PrimeiraChegada { get; }

    public Rede(List<Fila> filas, Dictionary<string, List<Rota>> rotas, double primeiraChegada)
    {
        if (filas.Count == 0)
            throw new ArgumentException("A network needs at least one queue.");
        if (primeiraChegada < 0)
            throw new ArgumentException("The first arrival cannot happen before time zero.");
        if (!filas.Any(f => f.HasExternalArrivals()))
            throw new ArgumentException("No queue receives clients from outside the network.");

        this.filas = filas;
        this.rotas = rotas;
        PrimeiraChegada = primeiraChegada;
    }

    public IReadOnlyList<Rota> RotasDe(Fila fila) => rotas[fila.Name];

    /// <summary>
    /// True when the destination is already known, so routing costs no random
    /// number. Holds for a queue that always exits the network and for one
    /// that always forwards to the same queue.
    /// </summary>
    public bool RoteamentoDeterministico(Fila fila)
    {
        List<Rota> destinos = rotas[fila.Name];
        return destinos.Count == 0 || (destinos.Count == 1 && destinos[0].Probabilidade >= 1.0);
    }

    /// <summary>
    /// Resolves where a client served by <paramref name="origem"/> goes.
    /// Returns null when the client leaves the network. The random number is
    /// only read when the routing is not deterministic.
    /// </summary>
    public Fila? Destino(Fila origem, double u = 0.0)
    {
        double acumulado = 0.0;

        foreach (Rota rota in rotas[origem.Name])
        {
            acumulado += rota.Probabilidade;
            if (u < acumulado)
                return rota.Destino;
        }

        return null;
    }

    public static Rede CarregarDe(string caminho)
    {
        string json = File.ReadAllText(caminho);

        ArquivoRede? arquivo = JsonSerializer.Deserialize<ArquivoRede>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

        if (arquivo is null || arquivo.Queues.Count == 0)
            throw new JsonException($"'{caminho}' does not describe any queue.");

        List<Fila> filas = arquivo.Queues.Select(CriaFila).ToList();

        var porNome = new Dictionary<string, Fila>();
        foreach (Fila fila in filas)
        {
            if (!porNome.TryAdd(fila.Name, fila))
                throw new ArgumentException($"Duplicated queue name: '{fila.Name}'.");
        }

        var rotas = new Dictionary<string, List<Rota>>();
        foreach (ArquivoFila descricao in arquivo.Queues)
            rotas[descricao.Name] = CriaRotas(descricao, porNome);

        return new Rede(filas, rotas, arquivo.FirstArrivalTime);
    }

    private static Fila CriaFila(ArquivoFila descricao)
    {
        int capacidade = descricao.Capacity ?? Fila.Unlimited;

        // A queue without an arrival range is fed only by other queues.
        return descricao.MinArrival.HasValue && descricao.MaxArrival.HasValue
            ? new Fila(descricao.Name, descricao.Servers, capacidade,
                       descricao.MinArrival.Value, descricao.MaxArrival.Value,
                       descricao.MinService, descricao.MaxService)
            : new Fila(descricao.Name, descricao.Servers, capacidade,
                       descricao.MinService, descricao.MaxService);
    }

    private static List<Rota> CriaRotas(ArquivoFila descricao, Dictionary<string, Fila> porNome)
    {
        var criadas = new List<Rota>();
        double total = 0.0;

        foreach (ArquivoRota rota in descricao.Routes)
        {
            Fila? destino = null;

            // An absent destination means the client leaves the network.
            if (!string.IsNullOrWhiteSpace(rota.To) && !porNome.TryGetValue(rota.To, out destino))
                throw new ArgumentException($"[{descricao.Name}] Unknown route destination: '{rota.To}'.");

            criadas.Add(new Rota(destino, rota.Probability));
            total += rota.Probability;
        }

        if (total > 1.0000001)
            throw new ArgumentException($"[{descricao.Name}] Route probabilities add up to {total:F4}, above 1.");

        return criadas;
    }

    private class ArquivoRede
    {
        public double FirstArrivalTime { get; set; }
        public List<ArquivoFila> Queues { get; set; } = [];
    }

    private class ArquivoFila
    {
        public string Name { get; set; } = string.Empty;
        public int Servers { get; set; }
        public int? Capacity { get; set; }
        public double? MinArrival { get; set; }
        public double? MaxArrival { get; set; }
        public double MinService { get; set; }
        public double MaxService { get; set; }
        public List<ArquivoRota> Routes { get; set; } = [];
    }

    private class ArquivoRota
    {
        public string? To { get; set; }
        public double Probability { get; set; } = 1.0;
    }
}
