using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueueSimulator;

public class Rota
{
    public int Destino { get; }

    public double Probabilidade { get; }

    public Rota(int destino, double probabilidade)
    {
        if (destino < Rede.Exterior)
            throw new ArgumentException($"Invalid route destination index: {destino}.");
        if (probabilidade <= 0 || probabilidade > 1)
            throw new ArgumentException("A route probability must be within (0, 1].");

        Destino = destino;
        Probabilidade = probabilidade;
    }
}

public class Rede
{
    public const int Exterior = -1;
    public const string NomeExterior = "Exterior";
    public const double ToleranciaProbabilidade = 1e-6;

    private readonly List<Fila> filas;
    private readonly List<List<Rota>> rotas;
    private readonly List<double?> primeirasChegadas;

    public IReadOnlyList<Fila> Filas => filas;

    public Rede(List<Fila> filas, List<List<Rota>> rotas, List<double?> primeirasChegadas)
    {
        if (filas.Count == 0)
            throw new ArgumentException("A network needs at least one queue.");
        if (rotas.Count != filas.Count || primeirasChegadas.Count != filas.Count)
            throw new ArgumentException("Every queue needs its own list of routes and first arrival.");
        if (!filas.Any(f => f.HasExternalArrivals()))
            throw new ArgumentException("No queue receives clients from outside the network.");

        for (int indice = 0; indice < filas.Count; indice++)
        {
            filas[indice].Indice = indice;
            ValidaRotas(filas[indice], rotas[indice], filas.Count);
            ValidaPrimeiraChegada(filas[indice], primeirasChegadas[indice]);
        }

        this.filas = filas;
        this.rotas = rotas;
        this.primeirasChegadas = primeirasChegadas;
    }

    // Time of the first external arrival, or null when the queue has no external arrivals.
    public double? PrimeiraChegadaDe(int indice) => primeirasChegadas[indice];

    public Fila FilaEm(int indice) => filas[indice];

    public string NomeDe(int indice) => indice == Exterior ? NomeExterior : filas[indice].Name;

    public IReadOnlyList<Rota> RotasDe(int origem) => rotas[origem];

    public bool RoteamentoDeterministico(int origem) => rotas[origem].Count == 1;

    public int Destino(int origem, double u)
    {
        List<Rota> destinos = rotas[origem];
        double acumulado = 0.0;

        foreach (Rota rota in destinos)
        {
            acumulado += rota.Probabilidade;
            if (u < acumulado)
                return rota.Destino;
        }

        // Only reachable when rounding leaves the sum a hair below 1.
        return destinos[^1].Destino;
    }

    private static void ValidaRotas(Fila fila, List<Rota> destinos, int totalFilas)
    {
        if (destinos.Count == 0)
            throw new ArgumentException(
                $"[{fila.Name}] No routes. Use {{ \"To\": \"{NomeExterior}\", \"Probability\": 1.0 }} for clients that leave the network.");

        if (destinos.Any(rota => rota.Destino >= totalFilas))
            throw new ArgumentException($"[{fila.Name}] A route points to a queue that does not exist.");

        if (destinos.Select(rota => rota.Destino).Distinct().Count() != destinos.Count)
            throw new ArgumentException($"[{fila.Name}] The same destination appears in more than one route.");

        double total = destinos.Sum(rota => rota.Probabilidade);
        if (Math.Abs(total - 1.0) > ToleranciaProbabilidade)
            throw new ArgumentException($"[{fila.Name}] Route probabilities add up to {total:F6}, they must add up to 1.");
    }

    private static void ValidaPrimeiraChegada(Fila fila, double? primeiraChegada)
    {
        if (fila.HasExternalArrivals() && !primeiraChegada.HasValue)
            throw new ArgumentException($"[{fila.Name}] Receives external arrivals but has no first arrival time.");
        if (!fila.HasExternalArrivals() && primeiraChegada.HasValue)
            throw new ArgumentException($"[{fila.Name}] Has a first arrival time but no MinArrival/MaxArrival.");
        if (primeiraChegada < 0)
            throw new ArgumentException($"[{fila.Name}] The first arrival cannot happen before time zero.");
    }

    public static Rede CarregarDe(string caminho)
    {
        string json = File.ReadAllText(caminho);

        ArquivoRede? arquivo = JsonSerializer.Deserialize<ArquivoRede>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            });

        if (arquivo is null)
            throw new JsonException($"'{caminho}' does not describe any queue.");

        return Monta(arquivo);
    }

    // Builds a network from a model description (shared by the JSON and YAML readers).
    internal static Rede Monta(ArquivoRede arquivo)
    {
        if (arquivo.Queues.Count == 0)
            throw new ArgumentException("The model does not describe any queue.");

        List<Fila> filas = arquivo.Queues.Select(CriaFila).ToList();

        var indicePorNome = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int indice = 0; indice < filas.Count; indice++)
        {
            if (filas[indice].Name.Equals(NomeExterior, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{NomeExterior}' is reserved for the outside of the network.");
            if (!indicePorNome.TryAdd(filas[indice].Name, indice))
                throw new ArgumentException($"Duplicated queue name: '{filas[indice].Name}'.");
        }

        List<List<Rota>> rotas = arquivo.Queues
            .Select(descricao => CriaRotas(descricao, indicePorNome))
            .ToList();

        List<double?> primeirasChegadas = arquivo.Queues
            .Select(descricao => descricao.MinArrival.HasValue
                ? descricao.FirstArrival ?? arquivo.FirstArrivalTime
                : descricao.FirstArrival)
            .ToList();

        return new Rede(filas, rotas, primeirasChegadas);
    }

    private static Fila CriaFila(ArquivoFila descricao)
    {
        int capacidade = descricao.Capacity ?? Fila.Unlimited;

        if (descricao.MinArrival.HasValue != descricao.MaxArrival.HasValue)
            throw new ArgumentException($"[{descricao.Name}] External arrivals need both MinArrival and MaxArrival.");

        return descricao.MinArrival.HasValue && descricao.MaxArrival.HasValue
            ? new Fila(descricao.Name, descricao.Servers, capacidade,
                       descricao.MinArrival.Value, descricao.MaxArrival.Value,
                       descricao.MinService, descricao.MaxService)
            : new Fila(descricao.Name, descricao.Servers, capacidade,
                       descricao.MinService, descricao.MaxService);
    }

    private static List<Rota> CriaRotas(ArquivoFila descricao, Dictionary<string, int> indicePorNome)
    {
        var criadas = new List<Rota>();

        foreach (ArquivoRota rota in descricao.Routes)
        {
            if (string.IsNullOrWhiteSpace(rota.To))
                throw new ArgumentException($"[{descricao.Name}] Every route needs \"To\" (a queue name or \"{NomeExterior}\").");
            if (!rota.Probability.HasValue)
                throw new ArgumentException($"[{descricao.Name}] The route to '{rota.To}' has no \"Probability\".");

            int destino;
            if (rota.To.Equals(NomeExterior, StringComparison.OrdinalIgnoreCase))
                destino = Exterior;
            else if (!indicePorNome.TryGetValue(rota.To, out destino))
                throw new ArgumentException($"[{descricao.Name}] Unknown route destination: '{rota.To}'.");

            criadas.Add(new Rota(destino, rota.Probability.Value));
        }

        return criadas;
    }

    internal class ArquivoRede
    {
        public double? FirstArrivalTime { get; set; }
        public List<ArquivoFila> Queues { get; set; } = [];
    }

    internal class ArquivoFila
    {
        public string Name { get; set; } = string.Empty;
        public required int Servers { get; set; }
        public int? Capacity { get; set; }
        public double? FirstArrival { get; set; }
        public double? MinArrival { get; set; }
        public double? MaxArrival { get; set; }
        public required double MinService { get; set; }
        public required double MaxService { get; set; }
        public List<ArquivoRota> Routes { get; set; } = [];
    }

    internal class ArquivoRota
    {
        public string? To { get; set; }
        public double? Probability { get; set; }
    }
}
