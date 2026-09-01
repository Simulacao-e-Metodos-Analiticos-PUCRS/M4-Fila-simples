using System;
using System.Collections.Generic;

namespace QueueSimulator;
public enum EventType
{
    Arrival,
    Departure
}

public class SimulationEvent
{
    public double Time { get; set; }
    public EventType Type { get; set; }
}

public class Simulator(
    uint servers,
    uint? maxCapacity,
    uint numberOfEvents,
    double firstArrivalTime,
    double minArrivalTime,
    double maxArrivalTime,
    double minServiceTime,
    double maxServiceTime)
{

    private uint? MaxCapacity { get; init; } = maxCapacity;
    private uint Servers { get; init; } = servers;
    private uint NumberOfEvents { get; init; } = numberOfEvents;
    private double FirstArrivalTime { get; init; } = firstArrivalTime;
    private double MinArrivalTime { get; init; } = minArrivalTime;
    private double MaxArrivalTime { get; init; } = maxArrivalTime;
    private double MinServiceTime { get; init; } = minServiceTime;
    private double MaxServiceTime { get; init; } = maxServiceTime;
    private bool IsCapacityUnlimited => !MaxCapacity.HasValue;
    private List<double> TimeInState { get; } = maxCapacity.HasValue
        ? new List<double>((int)(maxCapacity.Value + 1))
        : [];
    
    // Event queue
    private PriorityQueue<SimulationEvent, double> _eventQueue = new();

    private RandomGen RandomGenerator { get; } = new RandomGen();

    private bool _simulationEnded = false;
    private double _currentTime = 0.0;
    private int _numClients = 0;
    private double _lastEventTime = 0.0;
    private uint _processedEvents = 0u;
    private uint _debugEventNumber = 0u;
    private uint _randomsUsed = 0u;
    private const uint MaxRandoms = 100_000;
    private uint _unservedEvents = 0u;
    private double _totalServiceTime = 0.0;
    private uint _servedEvents = 0u;
    private uint _busyServers = 0u;

    public void Run(bool debug = false)
    {
        ValidateTimeRanges();

        if (debug)
        {
            ConsolePrinter.PrintSimulationParameters(
                Servers,
                MaxCapacity,
                NumberOfEvents,
                FirstArrivalTime,
                MinArrivalTime,
                MaxArrivalTime,
                MinServiceTime,
                MaxServiceTime);
            ConsolePrinter.PrintDebugHeader();
        }

        // 1. Initial condition: schedule the first arrival independently
        ScheduleEvent(FirstArrivalTime, EventType.Arrival);

        // 2. Main simulation loop
        while (_eventQueue.Count > 0 && !_simulationEnded)
        {
            var currentEvent = _eventQueue.Dequeue();
            _processedEvents++;
            _debugEventNumber++;

            UpdateStatistics(currentEvent.Time);
            _currentTime = currentEvent.Time;
            int clientsBeforeEvent = _numClients;

            bool unserved = currentEvent.Type == EventType.Arrival && ProcessArrival();
            if (currentEvent.Type == EventType.Departure)
                ProcessDeparture();

            int clientsForDebug = currentEvent.Type == EventType.Departure
                ? clientsBeforeEvent
                : _numClients;

            if (debug)
                ConsolePrinter.PrintDebugEvent(_debugEventNumber, currentEvent, clientsForDebug, unserved);
        }
    }

    private bool ProcessArrival()
    {
        bool accepted = IsCapacityUnlimited || _numClients < MaxCapacity;
        if (accepted)
        {
            _numClients++;
        }
        else
        {
            _unservedEvents++;
        }

        if (_simulationEnded) return !accepted;
        double prnArrival = GetNextPRN();
        if (_simulationEnded) return !accepted;

        double tec = MinArrivalTime + (MaxArrivalTime - MinArrivalTime) * prnArrival;
        ScheduleEvent(_currentTime + tec, EventType.Arrival);

        if (accepted && _busyServers < Servers)
        {
            if (_simulationEnded) return !accepted;
            double prnDeparture = GetNextPRN();
            if (_simulationEnded) return !accepted;

            double ts = MinServiceTime + (MaxServiceTime - MinServiceTime) * prnDeparture;
            _totalServiceTime += ts;
            _servedEvents++;
            _busyServers++;
            ScheduleEvent(_currentTime + ts, EventType.Departure);
        }

        return !accepted;
    }

    private void ProcessDeparture()
    {
        // Client leaves the system
        if (_numClients > 0)
        {
            _numClients--;
            _busyServers--;

            if (_numClients >= _busyServers + 1)
            {
                if (_simulationEnded) return;
                double prnDeparture = GetNextPRN();
                double ts = MinServiceTime + (MaxServiceTime - MinServiceTime) * prnDeparture;
                _totalServiceTime += ts;
                _servedEvents++;
                _busyServers++;
                ScheduleEvent(_currentTime + ts, EventType.Departure);
            }
        }
    }

    private void ScheduleEvent(double time, EventType type)
    {
        _eventQueue.Enqueue(new SimulationEvent { Time = time, Type = type }, time);
    }

    private double GetNextPRN()
    {
        if (_randomsUsed >= MaxRandoms)
        {
            _simulationEnded = true;
            return 0;
        }
        _randomsUsed++;
        return RandomGenerator.NextDouble();
    }

    private void UpdateStatistics(double newTime)
    {
        double timePassed = newTime - _lastEventTime;

        while (TimeInState.Count <= _numClients)
            TimeInState.Add(0.0);

        TimeInState[_numClients] += timePassed;
        _lastEventTime = newTime;
    }

    private void ValidateTimeRanges()
    {
        if (FirstArrivalTime < 0)
            throw new ArgumentException("The first arrival time cannot be negative.");
        if (MinArrivalTime < 0 || MaxArrivalTime < MinArrivalTime)
            throw new ArgumentException("Arrival time limits are invalid.");
        if (MinServiceTime < 0 || MaxServiceTime < MinServiceTime)
            throw new ArgumentException("Service time limits are invalid.");
    }

    public void PrintResults()
    {
        string notation = IsCapacityUnlimited ? $"G/G/{Servers}/∞" : $"G/G/{Servers}/{MaxCapacity}";
        double averageServiceTime = _servedEvents > 0 ? _totalServiceTime / _servedEvents : 0.0;

        ConsolePrinter.PrintResultsTable(notation, _currentTime, TimeInState, averageServiceTime, _processedEvents, _unservedEvents);
    }
}
