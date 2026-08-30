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
}
