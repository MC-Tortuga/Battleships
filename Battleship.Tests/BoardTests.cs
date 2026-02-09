using Battleship.Shared;
using Xunit;
namespace Battleship.Tests;
public class BoardTests
{
    [Fact]
    public void Board_NewBoard_IsEmpty()
    {
        var board = new Board();
        Assert.Empty(board.Ships);
    }
    [Fact]
    public void Board_PlaceShip_ValidPlacement_ReturnsTrue()
    {
        var board = new Board();
        var result = board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.True(result);
        Assert.Single(board.Ships);
    }
    [Fact]
    public void Board_PlaceShip_OverlappingPlacement_ReturnsFalse()
    {
        var board = new Board();
        var firstResult = board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.True(firstResult);
        var secondResult = board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.False(secondResult);
        Assert.Single(board.Ships);
    }
    [Fact]
    public void Board_PlaceShip_OutOfBounds_ReturnsFalse()
    {
        var board = new Board();
        var result = board.PlaceShip(ShipType.Carrier, new Coordinate(8, 0), Orientation.Horizontal);
        Assert.False(result);
        Assert.Empty(board.Ships);
    }
    [Fact]
    public void Board_CanPlaceShip_ValidCoordinates_ReturnsTrue()
    {
        var board = new Board();
        var result = board.CanPlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.True(result);
    }
    [Fact]
    public void Board_CanPlaceShip_OverlappingCoordinates_ReturnsFalse()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        var result = board.CanPlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.False(result);
    }
    [Fact]
    public void Board_FireShot_Hit_ReturnsHit()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        var result = board.FireShot(new Coordinate(0, 0));
        Assert.Equal(ShotResult.Hit, result);
    }
    [Fact]
    public void Board_FireShot_Miss_ReturnsMiss()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        var result = board.FireShot(new Coordinate(5, 5));
        Assert.Equal(ShotResult.Miss, result);
    }
    [Fact]
    public void Board_FireShot_SameCoordinate_ReturnsMiss()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        var firstResult = board.FireShot(new Coordinate(0, 0));
        Assert.Equal(ShotResult.Hit, firstResult);
        var secondResult = board.FireShot(new Coordinate(0, 0));
        Assert.Equal(ShotResult.Miss, secondResult);
    }
    [Fact]
    public void Board_AreAllShipsSunk_EmptyBoard_ReturnsFalse()
    {
        var board = new Board();
        var result = board.AreAllShipsSunk();
        Assert.False(result);
    }
    [Fact]
    public void Board_AreAllShipsSunk_ShipsPresent_ReturnsFalse()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.False(board.AreAllShipsSunk());
    }
    [Fact]
    public void Board_AreAllShipsSunk_ShipSunk_ReturnsTrue()
    {
        var board = new Board();
        board.PlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal);
        board.FireShot(new Coordinate(0, 0));
        board.FireShot(new Coordinate(1, 0));
        Assert.True(board.AreAllShipsSunk());
    }
    [Theory]
    [InlineData(ShipType.Carrier, 5)]
    [InlineData(ShipType.Battleship, 4)]
    [InlineData(ShipType.Destroyer, 3)]
    [InlineData(ShipType.PatrolBoat, 2)]
    public void Board_Ship_OccupiedCoordinates_CountMatchesShipSize(ShipType shipType, int expectedSize)
    {
        var ship = new Ship(shipType, new Coordinate(0, 0), Orientation.Horizontal);
        Assert.Equal(expectedSize, ship.OccupiedCoordinates.Count);
    }
    [Fact]
    public void Board_Ship_VerticalOrientation_CorrectCoordinates()
    {
        var ship = new Ship(ShipType.PatrolBoat, new Coordinate(2, 3), Orientation.Vertical);
        var expectedCoords = new[]
        {
            new Coordinate(2, 3),
            new Coordinate(2, 4)
        };
        Assert.Equal(expectedCoords.Length, ship.OccupiedCoordinates.Count);
        foreach (var expected in expectedCoords)
        {
            Assert.Contains(expected, ship.OccupiedCoordinates);
        }
    }
    [Fact]
    public void Board_Ship_HorizontalOrientation_CorrectCoordinates()
    {
        var ship = new Ship(ShipType.PatrolBoat, new Coordinate(2, 3), Orientation.Horizontal);
        var expectedCoords = new[]
        {
            new Coordinate(2, 3),
            new Coordinate(3, 3)
        };
        Assert.Equal(expectedCoords.Length, ship.OccupiedCoordinates.Count);
        foreach (var expected in expectedCoords)
        {
            Assert.Contains(expected, ship.OccupiedCoordinates);
        }
    }
}