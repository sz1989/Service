namespace Service.Tests;

public class NotedTests
{
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
