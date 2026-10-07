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

    public static void PrintNetworkResults(Rede rede, ResultadoSimulacao resultado, string titulo)
    {
        Console.WriteLine();
        PrintHeaderLine();
        PrintHeaderText($"QUEUEING NETWORK RESULTS - {rede.Filas.Count} QUEUE(S)");
        PrintHeaderText(titulo);
        PrintHeaderLine();

        Console.WriteLine();
        PrintField("Global simulation time", $"{resultado.TempoGlobal:F6} minutes");
        PrintField("Events processed", $"{resultado.Eventos:0.##}");
        PrintField("Random numbers used", $"{resultado.Aleatorios:0.##}");

        for (int indice = 0; indice < rede.Filas.Count; indice++)
        {
            Fila fila = rede.Filas[indice];
            ResultadoFila resultadoFila = resultado.Filas[indice];

            PrintSection($"{fila.Name} ({fila.Notation()})");
            if (rede.PrimeiraChegadaDe(indice) is double primeiraChegada)
            {
                PrintField("First arrival", $"{primeiraChegada:F2} minutes");
                PrintField("External arrivals", $"{fila.MinArrival():F2} .. {fila.MaxArrival():F2} minutes");
            }
            PrintField("Service time", $"{fila.MinService():F2} .. {fila.MaxService():F2} minutes");
            PrintField("Routing", DescribeRouting(rede, fila));
            PrintStateTable(resultadoFila.Tempos, resultado.TempoGlobal);
            PrintField("Lost clients", $"{resultadoFila.Perdas:0.##}");
        }
    }

    public static void PrintNetworkDebugHeader(Rede rede)
    {
        PrintHeaderText("EVENT DEBUG");
        PrintHeaderLine();
        Console.WriteLine("States after each event, in order: "
            + string.Join(", ", rede.Filas.Select(fila => fila.Name)));
        Console.WriteLine();
    }

    public static void PrintNetworkDebugEvent(uint eventNumber, Evento evento, Rede rede)
    {
        string rota = $"{rede.NomeDe(evento.Origem)} ({evento.Origem}) -> {rede.NomeDe(evento.Destino)} ({evento.Destino})";
        string estados = string.Join(" ", rede.Filas.Select(fila => $"{fila.Status(),3}"));

        Console.WriteLine($"| {eventNumber,6} | {evento.Tipo,-8} | {evento.Tempo,14:F4} | {rota,-34} | {estados} |");
    }

    private static void PrintStateTable(double[] tempos, double totalTime)
    {
        Console.WriteLine();
        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine("  | State | Accumulated time (min) | Probability        | Percent     |");
        Console.WriteLine($"  {ResultBorder}");

        double somaTempos = 0.0;
        double somaProbabilidades = 0.0;

        for (int state = 0; state < tempos.Length; state++)
        {
            double probabilidade = totalTime > 0 ? tempos[state] / totalTime : 0.0;
            somaTempos += tempos[state];
            somaProbabilidades += probabilidade;

            Console.WriteLine($"  | {state,5} | {tempos[state],22:F6} | {probabilidade,18:F12} | {probabilidade * 100,10:F6}% |");
        }

        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine($"  | TOTAL | {somaTempos,22:F6} | {somaProbabilidades,18:F12} | {somaProbabilidades * 100,10:F6}% |");
        Console.WriteLine($"  {ResultBorder}");
        Console.WriteLine();
    }

    private static string DescribeRouting(Rede rede, Fila fila)
        => string.Join(", ", rede.RotasDe(fila.Indice).Select(rota =>
            $"{rota.Probabilidade * 100:0.##}% -> {rede.NomeDe(rota.Destino)}"));
}
