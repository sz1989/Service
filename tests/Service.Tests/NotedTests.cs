using Service.Others;

namespace Service.Tests;

public class NotedTests
{
    [Fact]
    public void Sums_deltas_across_units_up_to_station()
    {
        var data = new[]
        {
            new TurnstileReading("Grand Central", "A1", At(0),   100),
            new TurnstileReading("Grand Central", "A1", At(4),   150),  // +50
            new TurnstileReading("Grand Central", "B2", At(0),   1000),
            new TurnstileReading("Grand Central", "B2", At(4),   1010),  // +10
        };
        var tmp = Ridership.EntriesPerStation(data);
        Assert.Equal(60, tmp["Grand Central"]);
        //Ridership.EntriesPerStation(data)["Grand Central"].Should().Be(60);
    }

    [Fact]
    public void Counter_reset_contributes_zero_not_negative()
    {
        var data = new[]
        {
            new TurnstileReading("Fulton St", "C3", At(0), 9_000_000),
            new TurnstileReading("Fulton St", "C3", At(4), 40),         // hardware swap
            new TurnstileReading("Fulton St", "C3", At(8), 90),         // +50
        };
        var tmp = Ridership.EntriesPerStation(data);
        Assert.Equal(50, tmp["Fulton St"]);
    }

    // also: single reading (no window) -> 0; out-of-order timestamps; garbage spike
    private static DateTime At(int h) => new DateTime(2026, 8, 31, 0, 0, 0).AddHours(h);

    [Fact]
    public void GetChange_ReturnsCorrectChange()
    {
        var result = Noted.GetChange(87).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal((25, 3), result[0]);
        Assert.Equal((10, 1), result[1]);
        Assert.Equal((1, 2), result[2]);
    }

    [Theory]
    [InlineData("()")]
    [InlineData("()[]{}")]
    [InlineData("{[]}")]
    [InlineData("")]
    public void VerifyString_BalancedBrackets_ReturnsTrue(string input)
    {
        Assert.True(Noted.VerifyString(input));
    }

    [Theory]
    [InlineData("(")]
    [InlineData("([)]")]
    [InlineData(")(")]
    [InlineData("(]")]
    public void VerifyString_UnbalancedBrackets_ReturnsFalse(string input)
    {
        Assert.False(Noted.VerifyString(input));
    }

    [Fact]
    public void TwoSum_FindsPair_ReturnsIndices()
    {
        var result = Noted.TwoSum([2, 7, 11, 15], target: 13);

        Assert.Equal([0, 2], result);
    }

    [Fact]
    public void TwoSum_PairNotAtStart_ReturnsCorrectIndices()
    {
        var result = Noted.TwoSum([3, 2, 4], target: 6);

        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void TwoSum_NoSolutionExists_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Noted.TwoSum([1, 2, 3], target: 100));
    }
}