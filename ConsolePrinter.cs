using System;

namespace QueueSimulator;

public static class ConsolePrinter
{
    private const int HeaderWidth = 60;

    public static void PrintHeaderLine() => Console.WriteLine(new string('=', HeaderWidth));

    public static void PrintHeaderText(string text)
    {
        int leftPadding = Math.Max(0, (HeaderWidth - text.Length) / 2);
        Console.WriteLine($"{text.PadLeft(text.Length + leftPadding).PadRight(HeaderWidth)}");
    }

    public static void PrintSimulationParameters(
        uint servers,
        uint? maxCapacity,
        uint numberOfEvents,
        double firstArrivalTime,
        double minArrivalTime,
        double maxArrivalTime,
        double minServiceTime,
        double maxServiceTime)
    {
        PrintHeaderLine();
        PrintHeaderText("SIMULATION PARAMETERS");
        PrintHeaderText($"Servers: {servers}");
        PrintHeaderText($"Maximum Capacity: {maxCapacity?.ToString() ?? "Unlimited"}");
        PrintHeaderText($"Number of Events: {numberOfEvents}");
        PrintHeaderText($"First Arrival Time: {firstArrivalTime:F2} minutes");
        PrintHeaderText($"Arrival Time Range: {minArrivalTime:F2} - {maxArrivalTime:F2} minutes");
        PrintHeaderText($"Service Time Range: {minServiceTime:F2} - {maxServiceTime:F2} minutes");
        PrintHeaderLine();
    }

    public static void PrintDebugEvent(uint eventNumber, SimulationEvent simulationEvent, int clients, bool unserved)
    {
        Console.WriteLine($"| {eventNumber,5} | {simulationEvent.Type,-9} | {simulationEvent.Time,20:F2} | {clients,7} | {(unserved ? "LOSS" : "OK"),-8} |");
    }

    public static void PrintDebugHeader()
    {
        PrintHeaderText("EVENT DEBUG");
        PrintHeaderLine();
        Console.WriteLine("+-------+-----------+----------------------+---------+----------+");
        Console.WriteLine("| Event | Type      | Time (minutes)       | Clients | Status   |");
        Console.WriteLine("+-------+-----------+----------------------+---------+----------+");
    }

    public static void PrintResultsTable(
        string notation,
        double totalTime,
        List<double> timeInState,
        double averageServiceTime,
        uint numberOfEvents,
        uint unservedEvents)
    {
        PrintHeaderText("\n");
        PrintHeaderLine();
        PrintHeaderText($"{notation} SIMULATION RESULTS");
        PrintHeaderLine();
        Console.WriteLine("+-------+----------------------+----------------+");
        Console.WriteLine("| State | Time (minutes)       | Probability    |");
        Console.WriteLine("+-------+----------------------+----------------+");

        for (int state = 0; state < timeInState.Count; state++)
        {
            double probability = totalTime > 0 ? timeInState[state] / totalTime : 0;
            Console.WriteLine($"| {state,5} | {timeInState[state],20:F2} | {probability * 100,13:F2}% |");
        }

        Console.WriteLine("+-------+----------------------+----------------+");
        Console.WriteLine($"Simulation average time: {totalTime:F2} minutes");
        Console.WriteLine($"Average Service Time: {averageServiceTime:F3} minutes");
        Console.WriteLine($"Total number of events: {numberOfEvents}");
        Console.WriteLine($"Number of losses: {unservedEvents}");
    }

    private const string ResultBorder =
        "+-------+------------------------+--------------------+-------------+";

    private static void PrintField(string label, string value)
        => Console.WriteLine($"  {(label + " ").PadRight(26, '.')} {value}");

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));
    }

    public static void PrintNetworkResults(Rede rede, double totalTime, uint events, uint randoms)
    {
        Console.WriteLine();
        PrintHeaderLine();
        PrintHeaderText("QUEUE RESULTS - QUEUES 1 AND 2");
        PrintHeaderLine();

        Console.WriteLine();
        PrintField("Global simulation time", $"{totalTime:F6} minutes");
        PrintField("Events processed", events.ToString());
        PrintField("Random numbers used", randoms.ToString());

        foreach (Fila fila in rede.Filas)
        {
            Console.WriteLine();
            Console.WriteLine($"  {fila.Name} ({fila.Notation()})");
            PrintStateTable(fila, totalTime);
            Console.WriteLine($"  Lost clients: {fila.Losses()}");
        }
    }

    private static void PrintStateTable(Fila fila, double totalTime)
    {
        Console.WriteLine();
        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine("  | State | Accumulated time (min) | Probability        | Percent     |");
        Console.WriteLine($"  {ResultBorder}");

        for (int state = 0; state < fila.StateCount(); state++)
        {
            double tempo = fila.TimeAt(state);
            double probabilidade = totalTime > 0 ? tempo / totalTime : 0.0;

            Console.WriteLine($"  | {state,5} | {tempo,22:F6} | {probabilidade,18:F12} | {probabilidade * 100,10:F6}% |");
        }

        (double somaTempos, double somaProbabilidades) = Totals(fila, totalTime);

        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine($"  | TOTAL | {somaTempos,22:F6} | {somaProbabilidades,18:F12} | {somaProbabilidades * 100,10:F6}% |");
        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine();
    }

    private static (double Tempos, double Probabilidades) Totals(Fila fila, double totalTime)
    {
        double tempos = 0.0;
        double probabilidades = 0.0;

        for (int state = 0; state < fila.StateCount(); state++)
        {
            tempos += fila.TimeAt(state);
            probabilidades += totalTime > 0 ? fila.TimeAt(state) / totalTime : 0.0;
        }

        return (tempos, probabilidades);
    }

    private static void PrintCheck(string label, double obtido, double esperado, string detalhe)
    {
        double diferenca = Math.Abs(obtido - esperado);
        string veredito = diferenca <= 1e-6 ? "OK" : "FAILED";

        Console.WriteLine($"  {(label + " ").PadRight(34, '.')} {detalhe}");
        Console.WriteLine($"  {new string(' ', 34)} difference = {diferenca:E3}  [{veredito}]");
    }

    private static string DescribeRouting(Rede rede, Fila fila)
    {
        IReadOnlyList<Rota> rotas = rede.RotasDe(fila);

        if (rotas.Count == 0)
            return "leaves the network";

        return string.Join(", ", rotas.Select(rota =>
            $"{rota.Probabilidade * 100:F0}% -> {rota.Destino?.Name ?? "out of the network"}"));
    }
}
