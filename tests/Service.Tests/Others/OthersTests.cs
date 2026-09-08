using FluentAssertions;
using Service.Others;

namespace Service.Tests.Others;

public class StationNamesTests
{
    [Fact]
    public void ProcessTap_FirstTimeRider_ChargesStandardFare()
    {
        // Arrange
        var calculator = new TransitFareCalculator();
        string riderId = "RIDER_123";
        decimal expectedFare = 2.90m;

        // Act
        decimal actualFare = calculator.ProcessTap(new MetroCard { Id = riderId });

        // Assert
        Assert.Equal(expectedFare, actualFare);
    }

    [Fact]
    public void ProcessTap_MultipleRides_AccumulatesChargesCorrectly()
    {
        // Arrange
        var calculator = new TransitFareCalculator();
        string riderId = "COMMUTER_ABC";

        // Act & Assert
        // 11 rides at $2.90 = $31.90
        // 12th 2.1
        for (int i = 0; i < 14; i++)
        {
            decimal fare = calculator.ProcessTap(new MetroCard { Id = riderId });
            if (i < 11)
            {
                Assert.Equal(2.90m, fare);
            }
            else if (i == 11)
            {
                // 12th ride should be free
                Assert.Equal(2.1m, fare);
            }
            else
            {
                // After 12 rides, fare should be free
                Assert.Equal(0.0m, fare);
            }
        }
    }

    [Theory]
    [InlineData("34 St - Penn Station")]
    [InlineData("34th St-Penn Sta.")]
    [InlineData("  34 ST PENN STATION  ")]
    [InlineData("34 street penn station")]      // already-canonical input
    public void Known_variants_of_penn_station_share_one_canonical_form(string raw)
    {
        Names.Canonicalize(raw).Should().Be("34 street penn station");
    }
}

public class LateTripsTests
{
    private static readonly DateTime Base = new(2026, 8, 31, 8, 0, 0);
    private static readonly TimeSpan Threshold = TimeSpan.FromMinutes(5);

    // ---- helpers --------------------------------------------------------
    private static DateTime T(int minutes) => Base.AddMinutes(minutes);

    private static Trips.StopTime Sched(string trip, string stop, int seq, int schedMin) =>
        new(trip, stop, seq, T(schedMin));

    private static Trips.Observation Obs(string trip, string stop, int actualMin) =>
        new(trip, stop, T(actualMin));

    private static List<Trips.TripDelay> Run(IEnumerable<Trips.StopTime> schedule, IEnumerable<Trips.Observation> observed) =>
        Trips.LateTrips(schedule, observed, Threshold).ToList();

    // ---- tests ---------------------------------------------------------

    [Fact]
    public void Trip_late_beyond_threshold_at_one_stop_is_reported()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 0), Sched("T1", "S2", 2, 10) };
        var observed = new[] { Obs("T1", "S1", 0), Obs("T1", "S2", 18) }; // +8 at S2

        Run(schedule, observed).Should().ContainSingle().Which.Should().Be(new Trips.TripDelay("T1", "S2", TimeSpan.FromMinutes(8)));
    }

    [Fact]
    public void Worst_delay_across_stops_is_the_one_reported()
    {
        var schedule = new[]
        {
            Sched("T1", "S1", 1, 0),
            Sched("T1", "S2", 2, 10),
            Sched("T1", "S3", 3, 20),
        };
        var observed = new[]
        {
            Obs("T1", "S1", 7),    // +7
            Obs("T1", "S2", 22),   // +12  <- worst
            Obs("T1", "S3", 29),   // +9
        };

        Run(schedule, observed).Should().ContainSingle()
            .Which.Should().Be(new Trips.TripDelay("T1", "S2", TimeSpan.FromMinutes(12)));
    }

    [Fact]
    public void Trip_within_threshold_everywhere_is_not_reported()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 0), Sched("T1", "S2", 2, 10) };
        var observed = new[] { Obs("T1", "S1", 3), Obs("T1", "S2", 14) }; // +3, +4

        Run(schedule, observed).Should().BeEmpty();
    }

    [Fact]
    public void Delay_exactly_at_threshold_is_not_late()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 0) };
        var observed = new[] { Obs("T1", "S1", 5) }; // +5 exactly; threshold is 5

        Run(schedule, observed).Should().BeEmpty(); // strictly greater than
    }

    [Fact]
    public void Missing_observation_counts_as_on_time()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 0), Sched("T1", "S2", 2, 10) };
        var observed = new[] { Obs("T1", "S1", 2) }; // nothing for S2

        Run(schedule, observed).Should().BeEmpty();
    }

    [Fact]
    public void Early_arrival_is_not_late()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 10) };
        var observed = new[] { Obs("T1", "S1", 3) }; // 7 min early -> -7

        Run(schedule, observed).Should().BeEmpty();
    }

    [Fact]
    public void Only_late_trips_are_returned_each_once()
    {
        var schedule = new[]
        {
            Sched("ontime", "S1", 1, 0),
            Sched("late",   "S1", 1, 0),
            Sched("late",   "S2", 2, 10),
        };
        var observed = new[]
        {
            Obs("ontime", "S1", 2),    // +2
            Obs("late",   "S1", 9),    // +9
            Obs("late",   "S2", 21),   // +11
        };

        var result = Run(schedule, observed);

        result.Should().ContainSingle();
        result[0].Should().Be(new Trips.TripDelay("late", "S2", TimeSpan.FromMinutes(11)));
    }

    [Fact]
    public void Multiple_late_trips_each_get_their_own_worst_stop()
    {
        var schedule = new[]
        {
            Sched("A", "S1", 1, 0), Sched("A", "S2", 2, 10),
            Sched("B", "S1", 1, 0), Sched("B", "S2", 2, 10),
        };
        var observed = new[]
        {
            Obs("A", "S1", 6),  Obs("A", "S2", 17),   // +6, +7  -> A worst S2 +7
            Obs("B", "S1", 12), Obs("B", "S2", 18),   // +12, +8 -> B worst S1 +12
        };

        Run(schedule, observed).Should().BeEquivalentTo(new[]
        {
            new Trips.TripDelay("A", "S2", TimeSpan.FromMinutes(7)),
            new Trips.TripDelay("B", "S1", TimeSpan.FromMinutes(12)),
        });
    }

    // [Fact]
    // public void Empty_inputs_produce_no_results()
    // {
    //     Run(Array.Empty<StopTime>(), Array.Empty<Observation>()).Should().BeEmpty();
    //     Run(new[] { Sched("T1", "S1", 1, 0) }, Array.Empty<Observation>()).Should().BeEmpty();
    // }

    [Fact]
    public void Observations_with_no_matching_schedule_row_are_ignored()
    {
        var schedule = new[] { Sched("T1", "S1", 1, 0) };
        var observed = new[]
        {
            Obs("T1", "S1", 1),         // on time
            Obs("T1", "GHOST", 999),    // stop not in schedule
            Obs("T2", "S1", 999),       // trip not in schedule
        };

        Run(schedule, observed).Should().BeEmpty(); // must not throw or count
    }
}

public class OthersTests
{
    private static readonly DateTime Base = new(2026, 8, 31, 10, 0, 0);

    // ---- helpers ----------------------------------------------------------

    // finite feed from ascending minute offsets: Feed("1", 0, 6, 12)
    private static IEnumerator<Arrival> Feed(string line, params int[] minuteOffsets) =>
        minuteOffsets.Select(m => new Arrival(line, Base.AddMinutes(m))).GetEnumerator();

    // endless feed: first train at +start, then every stepMinutes
    private static IEnumerator<Arrival> InfiniteFeed(string line, int start, int stepMinutes)
    {
        for (var m = start; ; m += stepMinutes)
            yield return new Arrival(line, Base.AddMinutes(m));
    }

    [Fact]
    public void Interleaves_multiple_feeds_in_time_order()
    {
        var feeds = new[]
        {
            Feed("1", 0, 6, 12),
            Feed("2", 2, 9),
            Feed("3", 1, 4, 11),
        };

        var merged = ArrivalMerger.MergeFeeds(feeds).ToList();

        merged.Select(a => a.Time.Minute).Should().Equal(0, 1, 2, 4, 6, 9, 11, 12);
        merged.Select(a => a.Line).Should().Equal("1", "3", "2", "3", "1", "2", "3", "1");
    }

    [Fact]
    public void Output_is_non_decreasing_in_time_and_drops_nothing()
    {
        var feeds = new[]
        {
            Feed("A", 0, 5, 30, 31),
            Feed("B", 1, 2, 40),
            Feed("C", 3, 3, 3, 50),   // repeated times inside one feed
        };

        var merged = ArrivalMerger.MergeFeeds(feeds).ToList();

        merged.Zip(merged.Skip(1))
              .Should().OnlyContain(p => p.First.Time <= p.Second.Time);
        merged.Should().HaveCount(11);
    }

    [Fact]
    public void Single_feed_passes_through_unchanged()
    {
        var feeds = new[] { Feed("7", 0, 4, 8, 12) };

        ArrivalMerger.MergeFeeds(feeds).Should().Equal(
            new Arrival("7", Base),
            new Arrival("7", Base.AddMinutes(4)),
            new Arrival("7", Base.AddMinutes(8)),
            new Arrival("7", Base.AddMinutes(12)));
    }

    [Fact]
    public void No_feeds_yields_empty_sequence()
    {
        ArrivalMerger.MergeFeeds(Array.Empty<IEnumerator<Arrival>>())
                     .Should().BeEmpty();
    }

    [Fact]
    public void Empty_feeds_are_skipped()
    {
        var feeds = new[]
        {
            Feed("A"),              // empty
            Feed("B", 5, 10),
            Feed("C"),              // empty
            Feed("D", 1),
        };

        ArrivalMerger.MergeFeeds(feeds).Select(a => a.Line)
                     .Should().Equal("D", "B", "B");
    }

    [Fact]
    public void Ties_across_feeds_keep_every_arrival()
    {
        var feeds = new[]
        {
            Feed("A", 10, 10),
            Feed("B", 10),
            Feed("C", 10),
        };

        var merged = ArrivalMerger.MergeFeeds(feeds).ToList();

        merged.Should().HaveCount(4);
        merged.Should().OnlyContain(a => a.Time == Base.AddMinutes(10));
        merged.Select(a => a.Line).OrderBy(x => x)
              .Should().Equal("A", "A", "B", "C");   // order among equal times is unspecified
    }

    [Fact]
    public void Is_lazy_does_not_over_consume_infinite_feeds()
    {
        var feeds = new[]
        {
            InfiniteFeed("A", start: 0, stepMinutes: 5),
            InfiniteFeed("B", start: 2, stepMinutes: 5),
        };

        // if MergeFeeds weren't lazy, this would never return
        var next5 = ArrivalMerger.MergeFeeds(feeds).Take(5).ToList();

        next5.Select(a => a.Line).Should().Equal("A", "B", "A", "B", "A");
        next5.Select(a => a.Time.Minute).Should().Equal(0, 2, 5, 7, 10);
    }
    }