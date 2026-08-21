using System;
using System.Collections.Concurrent;

public class TrainDelayProcessor
{
    // Thread-safe dictionary tracking the last known delay (in minutes) for each Train ID
    private readonly ConcurrentDictionary<string, int> _lastKnownDelays = new();

    /// <summary>
    /// Processes a single raw telemetry packet.
    /// Format: "TRAIN_ID,SCHEDULED_TIME_EPOCH_MINUTES,ACTUAL_TIME_EPOCH_MINUTES"
    /// Example: "Q_LINE_042,29480100,29480108" -> (Scheduled time: 29480100, Actual time: 29480108 -> 8 min delay)
    /// </summary>
    public void ProcessTelemetryStream(string rawPacket)
    {
        if (string.IsNullOrWhiteSpace(rawPacket)) return;

        // 1. Convert to ReadOnlySpan to prevent allocations during parsing
        ReadOnlySpan<char> packetSpan = rawPacket.AsSpan();
        var enumerator = packetSpan.Split(',');
        
        // TODO: 2. Parse the fields using span slicing instead of string.Split()
        var fields = packetSpan.Split(',');
        // Field 1: TRAIN_ID
        if (!enumerator.MoveNext()) return;
        var trainId = packetSpan[enumerator.Current];

        // Field 2: SCHEDULED_TIME_EPOCH_MINUTES
        if (!enumerator.MoveNext()) return;
        var scheduledSpan = packetSpan[enumerator.Current];

        // Field 3: ACTUAL_TIME_EPOCH_MINUTES
        if (!enumerator.MoveNext()) return;
        var actualSpan = packetSpan[enumerator.Current];

        // 3. Calculate the delay (ActualTime - ScheduledTime)
        if (!int.TryParse(scheduledSpan, out int scheduledTime) || 
            !int.TryParse(actualSpan, out int actualTime))
        {
            return; // Handle parsing failure
        }

        // TODO: 3. Calculate the delay (ActualTime - ScheduledTime)
        int currentDelay = actualTime - scheduledTime;
        
        // 3. .NET 9+ Allocation-free dictionary lookup using the character span
        var alternateLookup = _lastKnownDelays.GetAlternateLookup<ReadOnlySpan<char>>();

        // Looks up using the span. If missing, treat as no prior delay and store the string only once.
        if (!alternateLookup.TryGetValue(trainId, out int previousDelay))
        {
            previousDelay = currentDelay;
        }
        alternateLookup[trainId] = currentDelay;

        // 4. Alert check
        if (Math.Abs(currentDelay - previousDelay) > 5)
        {
            // Only convert to string if an actual alert condition is met
            TriggerDispatchAlert(trainId.ToString(), previousDelay, currentDelay);
        }
    }

    private void TriggerDispatchAlert(string trainId, int oldDelay, int newDelay)
    {
        Console.WriteLine($"[ALERT] Train {trainId} delay shifted significantly! Old: {oldDelay}m, New: {newDelay}m");
    }
}