using System;

namespace QueueSimulator;

public class Fila
{
    public const int Unlimited = int.MaxValue;

    private readonly int servers;
    private readonly int capacity;
    private readonly double minArrival;
    private readonly double maxArrival;
    private readonly double minService;
    private readonly double maxService;
    private readonly bool externalArrivals;

    private int customers;
    private int loss;
    private double[] times;

    public string Name { get; }

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

    public int Status() => customers;

    public int Capacity() => capacity;

    public int Servers() => servers;

    public void Loss() => loss++;

    public void In() => customers++;

    public void Out() => customers--;

    public int Losses() => loss;

    public double MinArrival() => minArrival;

    public double MaxArrival() => maxArrival;

    public double MinService() => minService;

    public double MaxService() => maxService;

    public bool HasExternalArrivals() => externalArrivals;

    public bool IsUnlimited() => capacity == Unlimited;

    public bool IsFull() => customers >= capacity;

    public int BusyServers() => Math.Min(customers, servers);

    public bool HasFreeServer() => BusyServers() < servers;

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
