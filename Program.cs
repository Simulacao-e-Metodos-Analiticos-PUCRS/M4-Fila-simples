using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using YamlDotNet.Core;

namespace QueueSimulator;

public class Program
{
    public const string PARAMETROS = "model.yml";
    public static void Main(string[] args)
    {
        ConsolePrinter.PrintHeaderLine();
        ConsolePrinter.PrintHeaderText("QUEUEING SIMULATOR");
        ConsolePrinter.PrintHeaderText("(August 2026)");
        ConsolePrinter.PrintHeaderText("by Augusto Sanhudo da Silva Knob");
        ConsolePrinter.PrintHeaderText("Carlos Eduardo Brito Mascarello");
        ConsolePrinter.PrintHeaderText("Matheus Hrymalak Souza");
        ConsolePrinter.PrintHeaderText("Olivia Maite Furquim Araujo Livak");
        ConsolePrinter.PrintHeaderLine();
        ConsolePrinter.PrintHeaderText("Developed during the undergraduate class on");
        ConsolePrinter.PrintHeaderText("Simulation and Analytical Methods (2026/2)");
        ConsolePrinter.PrintHeaderText("Taught by Prof. Afonso Sales at");
        ConsolePrinter.PrintHeaderText("Polytechnic School (PUCRS)");
        ConsolePrinter.PrintHeaderLine();

        if (args.Length > 0 && "--create-model".Equals(args[0], StringComparison.OrdinalIgnoreCase))
        {
            string modelFileName = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : PARAMETROS;
            CreateModelFile(modelFileName);
            WaitForInput();
            return;
        }
        else if (args.Length == 0)
        {
            InteractiveMenu();
            return;
        }

        bool debug = args.Length > 1 && "--debug".Equals(args[1], StringComparison.OrdinalIgnoreCase);

        try
        {
            RunModel(ResolveModelFile(args[0]), debug);
        }
        catch (Exception exception) when (IsModelError(exception))
        {
            Console.WriteLine($"Error: {exception.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static bool IsModelError(Exception exception)
        => exception is IOException or JsonException or YamlException or ArgumentException or InvalidOperationException;

    private static void RunModel(string modelPath, bool debug)
    {
        if (Modelo.IsYaml(modelPath))
        {
            RunNetwork(Modelo.CarregarYaml(modelPath), debug);
            return;
        }

        string json = File.ReadAllText(modelPath);

        if (IsNetworkFile(json))
        {
            RunNetwork(Modelo.CarregarJson(modelPath), debug);
            return;
        }

        SimulationParameters parameters = ParseParameters(json);

        Simulator simulator = CreateSimulator(parameters);
        simulator.Run(debug);
        simulator.PrintResults();
        InteractiveTerminal(parameters, debug);
    }

    // One run per seed (or a single run with the given random numbers); with more than
    // one run, the mean of the runs is printed at the end.
    private static void RunNetwork(Modelo modelo, bool debug)
    {
        var resultados = new List<ResultadoSimulacao>();

        for (int indice = 0; indice < modelo.Execucoes.Count; indice++)
        {
            Execucao execucao = modelo.Execucoes[indice];
            Rede rede = modelo.CriaRede();

            Simulador simulador = new(rede, execucao.CriaGerador(), execucao.MaxAleatorios);
            simulador.Executa(debug);

            ResultadoSimulacao resultado = simulador.Resultado();
            resultados.Add(resultado);

            ConsolePrinter.PrintNetworkResults(
                rede, resultado, $"Run {indice + 1} of {modelo.Execucoes.Count} ({execucao.Descricao})");
        }

        if (resultados.Count > 1)
            ConsolePrinter.PrintNetworkResults(
                modelo.CriaRede(), ResultadoSimulacao.Media(resultados), $"MEAN OF {resultados.Count} RUNS");
    }

    private static bool IsNetworkFile(string json)
    {
        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.EnumerateObject()
                .Any(property => "Queues".Equals(property.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static Simulator CreateSimulator(SimulationParameters parameters)
    {
        return new(
            parameters.Servers,
            parameters.MaxCapacity,
            parameters.NumberOfEvents,
            parameters.FirstArrivalTime,
            parameters.MinArrivalTime,
            parameters.MaxArrivalTime,
            parameters.MinServiceTime,
            parameters.MaxServiceTime);
    }

    private static void InteractiveTerminal(SimulationParameters parameters, bool debug)
    {
        while (true)
        {
            Console.Write("Enter command [R = run again, Q = quit]: ");
            string? command = Console.ReadLine()?.Trim();

            if ("Q".Equals(command, StringComparison.OrdinalIgnoreCase))
                return;

            if ("R".Equals(command, StringComparison.OrdinalIgnoreCase))
            {
                Simulator simulator = CreateSimulator(parameters);
                simulator.Run(debug);
                simulator.PrintResults();
                continue;
            }

            Console.WriteLine("Unknown command. Use R to run again or Q to quit.");
        }
    }

    private static void InteractiveMenu()
    {
        while (true)
        {
            ConsolePrinter.PrintHeaderText("INTERACTIVE MENU");
            ConsolePrinter.PrintHeaderText("1 - Run simulation");
            ConsolePrinter.PrintHeaderText("2 - Create model file");
            ConsolePrinter.PrintHeaderText("Q - Quit");
            ConsolePrinter.PrintHeaderLine();
            Console.Write("Select an option: ");

            string? option = Console.ReadLine()?.Trim();
            if ("Q".Equals(option, StringComparison.OrdinalIgnoreCase))
                return;

            if ("1".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                Console.Write($"Model file (Enter for {PARAMETROS}): ");
                string fileName = Console.ReadLine()?.Trim() ?? string.Empty;
                fileName = string.IsNullOrEmpty(fileName) ? PARAMETROS : fileName;

                Console.Write("Enable debug? (Y/N): ");
                bool debug = "Y".Equals(Console.ReadLine()?.Trim(), StringComparison.OrdinalIgnoreCase);

                try
                {
                    RunModel(ResolveModelFile(fileName), debug);
                }
                catch (Exception exception) when (IsModelError(exception))
                {
                    Console.WriteLine($"Error: {exception.Message}");
                    WaitForInput();
                }
            }
            else if ("2".Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                Console.Write($"Model file name (Enter for {PARAMETROS}): ");
                string fileName = Console.ReadLine()?.Trim() ?? string.Empty;
                CreateModelFile(string.IsNullOrEmpty(fileName) ? PARAMETROS : fileName);
                WaitForInput();
            }
            else
            {
                Console.WriteLine("Unknown option. Choose 1, 2 or Q.");
            }
        }
    }

    // A name without extension is looked up as .yml, .yaml and then .json.
    private static string ResolveModelFile(string name)
    {
        if (Path.HasExtension(name))
            return ResolveModelPath(name);

        foreach (string extension in new[] { ".yml", ".yaml", ".json" })
        {
            if (TryResolveModelPath(name + extension, out string path))
                return path;
        }

        throw new FileNotFoundException($"Model file '{name}' (.yml, .yaml or .json) was not found.", name);
    }

    private static string ResolveModelPath(string fileName)
        => TryResolveModelPath(fileName, out string path)
            ? path
            : throw new FileNotFoundException($"Model file '{fileName}' was not found.", fileName);

    private static bool TryResolveModelPath(string fileName, out string path)
    {
        path = fileName;
        if (File.Exists(fileName))
            return true;

        string? directory = AppContext.BaseDirectory;

        while (directory is not null)
        {
            path = Path.Combine(directory, fileName);

            if (File.Exists(path))
                return true;

            directory = Directory.GetParent(directory)?.FullName;
        }

        return false;
    }

    private static SimulationParameters ParseParameters(string json)
    {
        return JsonSerializer.Deserialize<SimulationParameters>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })
            ?? throw new JsonException("The JSON file does not contain valid simulation parameters.");
    }

    private static void WaitForInput()
    {
        Console.Write("Press Enter to close...");
        Console.ReadLine();
    }

    private static void CreateModelFile(string fileName)
    {
        fileName = Path.HasExtension(fileName) ? fileName : $"{fileName}.yml";

        string destination = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        if (File.Exists(destination))
        {
            Console.WriteLine($"The file '{destination}' already exists. No changes were made.");
            return;
        }

        string yamlModel = """
        !PARAMETERS

        # First external arrival of each queue that receives clients from outside.
        arrivals:
           Q1: 2.0

        # capacity is optional (omitted = unlimited); minArrival/maxArrival only for
        # queues with external arrivals.
        queues:
           Q1:
              servers: 1
              minArrival: 2.0
              maxArrival: 4.0
              minService: 1.0
              maxService: 2.0
           Q2:
              servers: 2
              capacity: 5
              minService: 4.0
              maxService: 6.0

        # Routing. What is left of 1 for a source leaves the network; it can also be
        # written explicitly with "target: Exterior".
        network:
        -  source: Q1
           target: Q2
           probability: 1.0
        -  source: Q2
           target: Q1
           probability: 0.3

        # One run per seed with rndnumbersPerSeed random numbers each
        # (or "rndnumbers:" with a fixed list of numbers in [0, 1)).
        rndnumbersPerSeed: 100000
        seeds:
        - 98765
        """;
        File.WriteAllText(destination, yamlModel);
        Console.WriteLine($"Model file '{destination}' created successfully.");
    }

}

public class SimulationParameters
{
    public required uint Servers { get; set; }
    public uint? MaxCapacity { get; set; }
    public required uint NumberOfEvents { get; set; }
    public required double FirstArrivalTime { get; set; }
    public required double MinArrivalTime { get; set; }
    public required double MaxArrivalTime { get; set; }
    public required double MinServiceTime { get; set; }
    public required double MaxServiceTime { get; set; }
}
