using System;

namespace QueueSimulator;

/// <summary>
/// A single queue of the network: its configuration plus its runtime state.
/// Groups the flat fields that currently live in Simulator, so that the
/// simulator can hold several queues instead of a single one.
/// </summary>
public class Fila
{
    /// <summary>Capacity value that means "no limit".</summary>
    public const int Unlimited = int.MaxValue;

    // Configuration
    private readonly int servers;
    private readonly int capacity;
    private readonly double minArrival;
    private readonly double maxArrival;
    private readonly double minService;
    private readonly double maxService;
    private readonly bool externalArrivals;

    // Runtime state
    private int customers;
    private int loss;
    private double[] times;

    public string Name { get; }

    /// <summary>Queue fed by an external arrival stream.</summary>
    public Fila(string name, int servers, int capacity,
                double minArrival, double maxArrival,
                double minService, double maxService)
        : this(name, servers, capacity, minService, maxService)
    {
        if (minArrival < 0 || maxArrival < minArrival)
            throw new ArgumentException($"[{name}] Arrival time limits are invalid.");

        this.minArrival = minArrival;
        this.maxArrival = maxArrival;
        externalArrivals = true;
    }

    /// <summary>Queue fed only by other queues, with no external arrivals.</summary>
    public Fila(string name, int servers, int capacity,
                double minService, double maxService)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A queue must have a name.");
        if (servers < 1)
            throw new ArgumentException($"[{name}] A queue needs at least one server.");
        if (capacity < 1)
            throw new ArgumentException($"[{name}] Capacity must be at least one client.");
        if (minService < 0 || maxService < minService)
            throw new ArgumentException($"[{name}] Service time limits are invalid.");

        Name = name;
        this.servers = servers;
        this.capacity = capacity;
        this.minService = minService;
        this.maxService = maxService;
        times = new double[capacity == Unlimited ? 1 : capacity + 1];
    }

    // --- Operations suggested by the assignment ---

    /// <summary>Number of clients currently in the queue.</summary>
    public int Status() => customers;

    public int Capacity() => capacity;

    public int Servers() => servers;

    /// <summary>Records one client lost because the queue was full.</summary>
    public void Loss() => loss++;

    /// <summary>Admits one client.</summary>
    public void In() => customers++;

    /// <summary>Removes one client.</summary>
    public void Out() => customers--;

    // --- Accessors ---

    public int Losses() => loss;

    public double MinArrival() => minArrival;

    public double MaxArrival() => maxArrival;

    public double MinService() => minService;

    public double MaxService() => maxService;

    /// <summary>False for queues that only receive clients from other queues.</summary>
    public bool HasExternalArrivals() => externalArrivals;

    public bool IsUnlimited() => capacity == Unlimited;

    public bool IsFull() => customers >= capacity;

    // A client starts being served as soon as it is admitted and a server is
    // free, so the number of busy servers is always min(customers, servers).
    public int BusyServers() => Math.Min(customers, servers);

    public bool HasFreeServer() => BusyServers() < servers;

    // --- Accumulated times per state ---

    /// <summary>Credits an elapsed interval to the state the queue is in.</summary>
    public void AccumulateTime(double elapsed)
    {
        if (customers >= times.Length)
            Array.Resize(ref times, customers + 1);

        times[customers] += elapsed;
    }

    public double[] Times() => times;

    public double TimeAt(int state) => state < times.Length ? times[state] : 0.0;

    public int StateCount() => times.Length;

    public string Notation() => $"G/G/{servers}/{(IsUnlimited() ? "∞" : capacity.ToString())}";
}
