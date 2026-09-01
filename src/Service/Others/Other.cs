using System.Globalization;

namespace Service.Others;

public static class Names
{
    /// <summary>
    /// Feeds spell stations inconsistently: "34 St - Penn Station", "34th St-Penn Sta.", " 34 ST PENN STATION ". Given a list of raw names, return each canonical station and how many raw variants mapped to it.
    /// </summary>
    private static readonly Dictionary<string, string> Abbrev = new(StringComparer.OrdinalIgnoreCase)
    {
        ["st"] = "street", ["ave"] = "avenue", ["sta"] = "station",["sq"] = "square", ["rd"] = "road", ["blvd"] = "boulevard",
    };
    public static string Canonicalize(string raw)
    {
        var input = raw.Trim().ToLowerInvariant().Split(new [] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries).Select(t => System.Text.RegularExpressions.Regex.Replace(t, @"(\d+)(st|nd|rd|th)$", "$1")).ToArray();
        for(int i = 0; i < input.Length; i++)
        {
            var word = input[i];
            if (Abbrev.TryGetValue(word, out var full))
            {
                // replace abbreviation with full word
                input[i] = full;
            }
        }
        return string.Join(" ", input);
    }
}

public static class Trips 
{
    public record StopTime(string TripId, string StopId, int StopSequence, DateTime ScheduledArrival);
    public record Observation(string TripId, string StopId, DateTime ActualArrival);
    public record TripDelay(string TripId, string WorstStopId, TimeSpan WorstDelay);

    // public static IEnumerable<TripDelay> LateTrips(IEnumerable<StopTime> schedule,
    public static IEnumerable<TripDelay> LateTrips(IEnumerable<StopTime> schedule,
        IEnumerable<Observation> observed, TimeSpan threshold)
        {
            var worstDelays = new List<TripDelay>();
            // group Observation by tripId and stopId for fast lookup
            var obsLookup = observed.ToLookup(o => (o.TripId, o.StopId), v => v.ActualArrival);
            // go through schedule, for each stop, check if there's an observation, compute delay, and track worst delay per trip
            foreach (var stop in schedule)
            {
                var key = (stop.TripId, stop.StopId);
                if (obsLookup.Contains(key))
                {
                    var actualArrival = obsLookup[key].First(); // assume one observation per stop
                    var delay = actualArrival - stop.ScheduledArrival;
                    if (delay > threshold)
                    {
                        worstDelays.Add(new TripDelay(stop.TripId, stop.StopId, delay));
                    }
                }
            }
            return worstDelays
                .GroupBy(d => d.TripId)
                .Select(g => g.OrderByDescending(d => d.WorstDelay).First());
        }
}

public record Arrival(string Line, DateTime Time);

public static class ArrivalMerger
{
    public static IEnumerable<Arrival> MergeFeeds(IReadOnlyList<IEnumerator<Arrival>> feeds)
    {
        var heap = new PriorityQueue<(IEnumerator<Arrival> Feed, Arrival Item), DateTime>();

        foreach (var feed in feeds)
            if (feed.MoveNext())
                heap.Enqueue((feed, feed.Current), feed.Current.Time);

        while (heap.Count > 0)
        {
            var (feed, item) = heap.Dequeue();
            yield return item;

            if (feed.MoveNext())
                heap.Enqueue((feed, feed.Current), feed.Current.Time);
        }
    }
}

public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _lru = new(); // front = most recent

    public LruCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _map = new(capacity);
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);          // touch: move to front
            value = node.Value.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public void Put(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            existing.Value = (key, value);
            _lru.Remove(existing);
            _lru.AddFirst(existing);
            return;
        }

        if (_map.Count == _capacity)
        {
            var victim = _lru.Last!;      // least recently used
            _lru.RemoveLast();
            _map.Remove(victim.Value.Key);
        }

        var node = new LinkedListNode<(TKey, TValue)>((key, value));
        _lru.AddFirst(node);
        _map[key] = node;
    }
}

public readonly ref struct VehicleTelemetry
{
    public ReadOnlySpan<char> VehicleId { get; init; }
    public long Timestamp { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int SpeedMph { get; init; }
}

public class TelemetryProcessor
{
    /// <summary>
    /// Parses a raw string telemetry packet using allocation-free Span manipulation.
    /// </summary>
    /// "MTA_BUS_8421|1788220400|40.7128|-74.0060|14"
    /// <param name="rawPacket">The raw string payload from the network.</param>
    /// <param name="result">The out parameter populated with the parsed data if successful.</param>
    /// <returns>True if parsing succeeded entirely; false if the packet was malformed.</returns>
    public bool TryParseTelemetry(string rawPacket, out VehicleTelemetry result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(rawPacket)) return false;

        // Convert the string to a Span to execute stack-allocated slicing
        ReadOnlySpan<char> span = rawPacket.AsSpan();

        try
        {
            var index = span.IndexOf('|');
            if (index <= 0) return false;
            var vehicleId = span[..index];
            span = span[(index + 1)..];

            index = span.IndexOf('|');
            if (index <= 0 || !long.TryParse(span[..index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp)) return false;
            span = span[(index + 1)..];

            index = span.IndexOf('|');
            if (index <= 0 || !double.TryParse(span[..index], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)) return false;
            span = span[(index + 1)..];

            index = span.IndexOf('|');
            if (index <= 0 || !double.TryParse(span[..index], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)) return false;
            span = span[(index + 1)..];

            if (span.IsEmpty || span.IndexOf('|') >= 0 || !int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var speedMph)) return false;

            result = new VehicleTelemetry
            {
                VehicleId = vehicleId,
                Timestamp = timestamp,
                Latitude = latitude,
                Longitude = longitude,
                SpeedMph = speedMph
            };
            return true;
        }
        catch
        {
            // Fail-safe to ensure corrupted packets never crash the service loop
            return false;
        }
    }
}

public record TurnstileReading(string Station, string UnitId, DateTime Timestamp, long CumulativeEntries);

public static class Ridership
{
    public static IReadOnlyDictionary<string, long> EntriesPerStation(
        IEnumerable<TurnstileReading> readings,
        long maxPlausibleDelta = 100_000)
    {
        var foo = readings.GroupBy(r => (r.Station, r.UnitId)).ToArray();
 
        return readings
            .GroupBy(r => (r.Station, r.UnitId))
            .SelectMany(unit =>
            {
                var ordered = unit.OrderBy(r => r.Timestamp).ToList();

                // pair each reading with the next one (window edges)
                return ordered.Zip(ordered.Skip(1), (prev, curr) =>
                {
                    long delta = curr.CumulativeEntries - prev.CumulativeEntries;
                    if (delta < 0 || delta > maxPlausibleDelta)
                        delta = 0;                     // reset or bad read
                    return (curr.Station, Entries: delta);
                });
            })
            .GroupBy(x => x.Station)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Entries));
    }
}

public readonly record struct Money(decimal Amount)
{
    public static readonly Money Zero = new(0m);
    public static Money Dollars(decimal d) => new(d);
}

public record TapContext(
    string CardId,
    DateTime TapTime,
    string StationId,
    string RouteType,          // "subway", "local_bus", "express_bus"
    DateTime? LastPaidTapTime,
    string?   LastPaidRouteType);

public record FareResult(Money Charged, string Reason);

public interface IFareSchedule
{
    Money GetFare(TapContext ctx);
}

public interface ITransferPolicy
{
    bool IsTransfer(TapContext ctx);
}
public interface IMetroCard
{
    FareResult Evaluate(TapContext ctx, IFareSchedule schedule, ITransferPolicy transfers);
}

public class MetroCard : IMetroCard
{
    public FareResult Evaluate(TapContext ctx, IFareSchedule schedule, ITransferPolicy transfers)
    {
        if (ctx.LastPaidTapTime.HasValue && transfers.IsTransfer(ctx))
        {
            return new FareResult(Money.Zero, "Transfer");
        }
        else
        {
            var fare = schedule.GetFare(ctx);
            return new FareResult(fare, "Standard Fare");
        }
    }
}

// public static main 
// {
            
// }
