using System.Collections.Concurrent;

public class TransactionResult
{
    public bool IsValid { get; set; }
    public decimal FareCharged { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public interface IFareMedia
{
    string Id { get; }
    string MediaType { get; }
    TransactionResult ProcessSwipe(DateTime swipeTime, string chargeKind);
}

public class PayPerRide : IFareMedia
{
    private decimal _balance;
    private const decimal StandardFare = 2.90m;

    public string Id { get; }
    public string MediaType => "PayPerRide";

    public PayPerRide(string id, decimal initialBalance)
    {
        Id = id;
        _balance = initialBalance;
    }

    public TransactionResult ProcessSwipe(DateTime swipeTime, string chargeKind)
    {
        if (_balance < StandardFare)
        {
            return new TransactionResult { IsValid = false, ErrorMessage = "Insufficient Funds" };
        }

        _balance -= StandardFare;
        return new TransactionResult { IsValid = true, FareCharged = StandardFare };
    }
}

public class SevenDayPass : IFareMedia
{
    private DateTime? _firstUsedTime;

    public string Id { get; }
    public string MediaType => "7DayPass";

    public SevenDayPass(string id) => Id = id;

    public TransactionResult ProcessSwipe(DateTime swipeTime, string chargeKind)
    {
        // Activate on first use
        if (_firstUsedTime == null)
        {
            _firstUsedTime = swipeTime;
        }

        if (swipeTime > _firstUsedTime.Value.AddDays(7))
        {
            return new TransactionResult { IsValid = false, ErrorMessage = "Card Expired" };
        }

        return new TransactionResult { IsValid = true, FareCharged = 0m };
    }
}

public class OMNY : IFareMedia
{
    private const decimal StandardFare = 2.90m;
    private const int CapThreshold = 12;

    // Store the history of paid swipes
    private readonly List<DateTime> _history = new();

    public string Id { get; }
    public string MediaType => "OMNY";

    public OMNY(string id) => Id = id;

    public TransactionResult ProcessSwipe(DateTime swipeTime, string chargeKind)
    {
        // 1. Calculate the start of the current calendar week (Monday)
        int daysSinceMonday = ((int)swipeTime.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        DateTime startOfWeek = swipeTime.Date.AddDays(-daysSinceMonday);

        // 2. Count how many times they swiped THIS specific week BEFORE this current swipe
        int swipesThisWeek = _history.Count(h => h >= startOfWeek);

        if (swipesThisWeek >= CapThreshold)
        {
            // Cap achieved! Ride is free, but we still log it
            _history.Add(swipeTime);
            return new TransactionResult { IsValid = true, FareCharged = 0m };
        }

        // 3. Otherwise, charge them and log the paid swipe
        _history.Add(swipeTime);
        return new TransactionResult { IsValid = true, FareCharged = StandardFare };
    }
}

public interface ITransportation
{
    public string Id { get; }
    public ChargeKind Kind { get; }
    public PaymentResult TakePayment(IFareMedia media);
}

public class Turnstile : ITransportation
{
    public ChargeKind Kind { get; }
    public string Id { get; }

    // Using a strongly typed class for history is cleaner than complex tuples
    private readonly ConcurrentBag<RideLog> _history = new();

    public Turnstile(string id)
    {
        Kind = ChargeKind.Turnstile;
        Id = id;
    }
    public PaymentResult TakePayment(IFareMedia media)
    {
        DateTime now = DateTime.UtcNow;

        // Polymorphic call: The turnstile does not care about the card type logic!
        TransactionResult result = media.ProcessSwipe(now, Kind.ToString());

        if (!result.IsValid)
        {
            return new PaymentResult { Success = false, Message = result.ErrorMessage };
        }

        // Log successful entries
        _history.Add(new RideLog(media.Id, media.MediaType, result.FareCharged, now));

        return new PaymentResult { Success = true, Message = "Go" };
    }

    public decimal GetTotalRevenue(DateTime from)
    {
        return _history.Where(h => h.Timestamp >= from).Sum(h => h.AmountCharged);
    }

    public int GetRiders(DateTime from)
    {
        return _history.Where(h => h.Timestamp >= from).Count();
    }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public record RideLog(string MediaId, string MediaType, decimal AmountCharged, DateTime Timestamp);

public enum ChargeKind
{
    Turnstile
}
