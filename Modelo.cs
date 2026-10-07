using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QueueSimulator;

// One simulation run: where its random numbers come from and how many it may use.
public record Execucao(string Descricao, Func<IGeradorAleatorio> CriaGerador, uint MaxAleatorios);

// A network model plus the runs to perform. Each run gets a fresh network (empty queues).
public class Modelo
{
    public const uint AleatoriosPadrao = 100_000;

    private readonly Func<Rede> criaRede;

    public IReadOnlyList<Execucao> Execucoes { get; }

    public Modelo(Func<Rede> criaRede, IReadOnlyList<Execucao> execucoes)
    {
        if (execucoes.Count == 0)
            throw new ArgumentException("The model needs at least one simulation run.");

        this.criaRede = criaRede;
        Execucoes = execucoes;

        // Builds the network once so that invalid models fail before any run.
        criaRede();
    }

    public Rede CriaRede() => criaRede();

    public static bool IsYaml(string caminho)
        => Path.GetExtension(caminho).ToLowerInvariant() is ".yml" or ".yaml";

    public static Modelo CarregarJson(string caminho)
        => new(() => Rede.CarregarDe(caminho),
               [new Execucao($"seed {RandomGen.SementePadrao}", () => new RandomGen(), AleatoriosPadrao)]);

    // Reads the model format used by the simulator of module 3:
    //
    //   !PARAMETERS
    //   arrivals:            first external arrival of each queue
    //   queues:              servers, capacity, minArrival, maxArrival, minService, maxService
    //   network:             source, target, probability (what is left goes to the outside)
    //   rndnumbersPerSeed:   random numbers per run   } or rndnumbers: a fixed list
    //   seeds:               one run per seed         }
    public static Modelo CarregarYaml(string caminho)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTagMapping("!PARAMETERS", typeof(ArquivoYaml))
            .Build();

        ArquivoYaml arquivo = deserializer.Deserialize<ArquivoYaml?>(File.ReadAllText(caminho))
            ?? throw new ArgumentException($"'{caminho}' is empty.");

        Rede.ArquivoRede descricao = ConverteRede(arquivo);

        return new Modelo(() => Rede.Monta(descricao), CriaExecucoes(arquivo));
    }

    private static Rede.ArquivoRede ConverteRede(ArquivoYaml arquivo)
    {
        if (arquivo.Queues.Count == 0)
            throw new ArgumentException("The model does not describe any queue ('queues').");

        foreach (string nome in arquivo.Arrivals.Keys.Where(nome => !arquivo.Queues.ContainsKey(nome)))
            throw new ArgumentException($"'arrivals' refers to an unknown queue: '{nome}'.");

        foreach (RotaYaml rota in arquivo.Network)
        {
            if (string.IsNullOrWhiteSpace(rota.Source) || !arquivo.Queues.ContainsKey(rota.Source))
                throw new ArgumentException($"'network' has a route with an unknown source: '{rota.Source}'.");
        }

        var filas = new List<Rede.ArquivoFila>();

        foreach ((string nome, FilaYaml fila) in arquivo.Queues)
        {
            filas.Add(new Rede.ArquivoFila
            {
                Name = nome,
                Servers = fila.Servers ?? throw new ArgumentException($"[{nome}] 'servers' is missing."),
                Capacity = fila.Capacity,
                FirstArrival = arquivo.Arrivals.TryGetValue(nome, out double primeira) ? primeira : null,
                MinArrival = fila.MinArrival,
                MaxArrival = fila.MaxArrival,
                MinService = fila.MinService ?? throw new ArgumentException($"[{nome}] 'minService' is missing."),
                MaxService = fila.MaxService ?? throw new ArgumentException($"[{nome}] 'maxService' is missing."),
                Routes = ConverteRotas(nome, arquivo.Network.Where(rota => rota.Source == nome).ToList())
            });
        }

        return new Rede.ArquivoRede { Queues = filas };
    }

    // As in the module 3 simulator, whatever probability is left after the listed routes
    // leaves the network. A route with target "Exterior" may also be written explicitly;
    // in that case the routes of the queue must add up to 1 by themselves.
    private static List<Rede.ArquivoRota> ConverteRotas(string origem, List<RotaYaml> rotas)
    {
        var convertidas = new List<Rede.ArquivoRota>();

        foreach (RotaYaml rota in rotas)
        {
            if (string.IsNullOrWhiteSpace(rota.Target))
                throw new ArgumentException($"[{origem}] A route in 'network' has no 'target'.");
            if (!rota.Probability.HasValue)
                throw new ArgumentException($"[{origem}] The route to '{rota.Target}' has no 'probability'.");

            convertidas.Add(new Rede.ArquivoRota { To = rota.Target, Probability = rota.Probability });
        }

        bool saidaExplicita = convertidas.Any(rota =>
            rota.To!.Equals(Rede.NomeExterior, StringComparison.OrdinalIgnoreCase));
        double restante = 1.0 - convertidas.Sum(rota => rota.Probability!.Value);

        if (!saidaExplicita && restante > Rede.ToleranciaProbabilidade)
            convertidas.Add(new Rede.ArquivoRota { To = Rede.NomeExterior, Probability = restante });

        return convertidas;
    }

    private static List<Execucao> CriaExecucoes(ArquivoYaml arquivo)
    {
        if (arquivo.Rndnumbers is { Count: > 0 } numeros)
        {
            if (arquivo.Seeds is { Count: > 0 } || arquivo.RndnumbersPerSeed.HasValue)
                throw new ArgumentException("Use either 'rndnumbers' or 'seeds'/'rndnumbersPerSeed', not both.");

            double[] lista = numeros.ToArray();
            return [new Execucao($"{lista.Length} given random numbers", () => new ListaAleatorios(lista), (uint)lista.Length)];
        }

        uint porSemente = arquivo.RndnumbersPerSeed ?? AleatoriosPadrao;
        if (porSemente == 0)
            throw new ArgumentException("'rndnumbersPerSeed' must be greater than zero.");

        List<uint> sementes = arquivo.Seeds is { Count: > 0 } ? arquivo.Seeds : [RandomGen.SementePadrao];

        return sementes
            .Select(semente => new Execucao($"seed {semente}", () => new RandomGen(semente), porSemente))
            .ToList();
    }

    internal sealed class ArquivoYaml
    {
        public Dictionary<string, double> Arrivals { get; set; } = [];
        public Dictionary<string, FilaYaml> Queues { get; set; } = [];
        public List<RotaYaml> Network { get; set; } = [];
        public uint? RndnumbersPerSeed { get; set; }
        public List<uint>? Seeds { get; set; }
        public List<double>? Rndnumbers { get; set; }
    }

    internal sealed class FilaYaml
    {
        public int? Servers { get; set; }
        public int? Capacity { get; set; }
        public double? MinArrival { get; set; }
        public double? MaxArrival { get; set; }
        public double? MinService { get; set; }
        public double? MaxService { get; set; }
    }

    internal sealed class RotaYaml
    {
        public string? Source { get; set; }
        public string? Target { get; set; }
        public double? Probability { get; set; }
    }
}
