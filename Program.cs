using System;
using System.IO;
using System.Text.Json;

namespace QueueSimulator;

public class Program
{
    public const string PARAMETROS = "model.json";
    public static void Main(string[] args)
    {
        ConsolePrinter.PrintHeaderLine();
        ConsolePrinter.PrintHeaderText("QUEUEING SIMULATOR");
        ConsolePrinter.PrintHeaderText("version 1.0");
        ConsolePrinter.PrintHeaderText("(August 2026)");
        ConsolePrinter.PrintHeaderText("by Augusto Sanhudo da Silva Knob");
        ConsolePrinter.PrintHeaderText("Carlos Eduardo Brito Mascarello");
        ConsolePrinter.PrintHeaderText("Matheus Hrymalak Souza");
        ConsolePrinter.PrintHeaderText("Olivia Maite Furquim Araujo Livak");
        ConsolePrinter.PrintHeaderLine();
        ConsolePrinter.PrintHeaderText("Developed during the undergraduate class on");
        ConsolePrinter.PrintHeaderText("Simulation and Analytical Methods (2026/2)");
        ConsolePrinter.PrintHeaderText("Taught by Prof. Afonso Sales at");
        ConsolePrinter.PrintHeaderText("Polytechnic School (EP/PUCRS)");
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

        string fileName = args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? args[0] : $"{args[0]}.json";
        string json = File.ReadAllText(fileName);

        SimulationParameters parameters = JsonSerializer.Deserialize<SimulationParameters>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            })
            ?? throw new JsonException("The JSON file does not contain valid simulation parameters.");

        bool debug = args.Length > 1 && "--debug".Equals(args[1], StringComparison.OrdinalIgnoreCase);

        Simulator simulator = CreateSimulator(parameters);
        simulator.Run(debug);
        simulator.PrintResults();
        InteractiveTerminal(parameters, debug);
    }

    private static Simulator CreateSimulator(SimulationParameters parameters)
    {
        return new(
            parameters.Servers,
            parameters.MaxCapacity,
            parameters.PrnList,
            parameters.NumberOfEvents,
            parameters.RandomSeed,
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
                fileName = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.json";

                Console.Write("Enable debug? (Y/N): ");
                bool debug = "Y".Equals(Console.ReadLine()?.Trim(), StringComparison.OrdinalIgnoreCase);

                try
                {
                    SimulationParameters parameters = LoadParameters(fileName);
                    Simulator simulator = CreateSimulator(parameters);
                    simulator.Run(debug);
                    simulator.PrintResults();
                    InteractiveTerminal(parameters, debug);
                }
                catch (Exception exception) when (exception is IOException or JsonException or ArgumentException)
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

    private static SimulationParameters LoadParameters(string fileName)
    {
        string json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<SimulationParameters>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
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
        fileName = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.json";

        if (File.Exists(fileName))
        {
            Console.WriteLine($"The file '{fileName}' already exists. No changes were made.");
            return;
        }

        string jsonModel = """
        {
            /* Number of parallel servers in the queueing system. */
            "Servers": 2,

            /* Maximum number of clients in the system. Use null for unlimited capacity. */
            "MaxCapacity": 2,

            /* Number of events to process in the simulation. */
            "NumberOfEvents": 100,

            /* Seed used to choose reproducible values from the PRN list. Use null for random behavior. */
            "RandomSeed": null,

            /* Time of the first arrival event. */
            "FirstArrivalTime": 1.0,

            /* Minimum and maximum time between arrivals. */
            "MinArrivalTime": 1.0,
            "MaxArrivalTime": 2.0,

            /* Minimum and maximum client service time. */
            "MinServiceTime": 2.0,
            "MaxServiceTime": 3.0,

            /* Pseudo-random numbers used to generate arrival and service times.
               Each value must be between 0.0 and 1.0. */
            "PrnList": [
                0.5,
                0.1,
                0.9,
                0.1,
                0.2,
                0.8,
                0.1
            ]
        }
        """;

        File.WriteAllText(fileName, jsonModel);
        Console.WriteLine($"Model file '{fileName}' created successfully.");
    }

}

public class SimulationParameters
{
    public required uint Servers { get; set; }
    public uint? MaxCapacity { get; set; }
    public required uint NumberOfEvents { get; set; }
    public int? RandomSeed { get; set; }
    public required double FirstArrivalTime { get; set; }
    public required double MinArrivalTime { get; set; }
    public required double MaxArrivalTime { get; set; }
    public required double MinServiceTime { get; set; }
    public required double MaxServiceTime { get; set; }
    public required double[] PrnList { get; set; } = [];
}
