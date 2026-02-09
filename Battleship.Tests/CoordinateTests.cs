using Battleship.Shared;
using Xunit;
namespace Battleship.Tests;
public class CoordinateTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(5, 5, true)]
    [InlineData(9, 9, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(10, 0, false)]
    [InlineData(0, 10, false)]
    [InlineData(-5, -5, false)]
    [InlineData(15, 15, false)]
    public void Coordinate_IsValid_ReturnsExpectedValue(int x, int y, bool expectedValid)
    {
        var coord = new Coordinate(x, y);
        Assert.Equal(expectedValid, coord.IsValid);
    }
    [Fact]
    public void Coordinate_Equality_SameCoordinates_AreEqual()
    {
        var coord1 = new Coordinate(5, 3);
        var coord2 = new Coordinate(5, 3);
        Assert.Equal(coord1, coord2);
        Assert.True(coord1 == coord2);
        Assert.False(coord1 != coord2);
    }
    [Fact]
    public void Coordinate_Equality_DifferentCoordinates_AreNotEqual()
    {
        var coord1 = new Coordinate(5, 3);
        var coord2 = new Coordinate(3, 5);
        Assert.NotEqual(coord1, coord2);
        Assert.False(coord1 == coord2);
        Assert.True(coord1 != coord2);
    }
    [Fact]
    public void Coordinate_HashCode_SameCoordinates_AreEqual()
    {
        var coord1 = new Coordinate(5, 3);
        var coord2 = new Coordinate(5, 3);
        Assert.Equal(coord1.GetHashCode(), coord2.GetHashCode());
    }
    [Fact]
    public void Coordinate_ToString_ReturnsCorrectFormat()
    {
        var coord = new Coordinate(5, 3);
        var result = coord.ToString();
        Assert.Contains("X = 5", result);
        Assert.Contains("Y = 3", result);
        Assert.Contains("IsValid = True", result);
    }
    [Fact]
    public void Coordinate_DictionaryKey_WorksCorrectly()
    {
        var coord1 = new Coordinate(5, 3);
        var coord2 = new Coordinate(5, 3);
        var coord3 = new Coordinate(3, 5);
        var dict = new Dictionary<Coordinate, string>
        {
            [coord1] = "first"
        };
        Assert.True(dict.TryGetValue(coord2, out var value));
        Assert.Equal("first", value);
        Assert.False(dict.ContainsKey(coord3));
    }
}